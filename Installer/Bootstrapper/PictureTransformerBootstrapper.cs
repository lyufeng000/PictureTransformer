using System;
using System.IO;
using System.Windows.Interop;
using System.Windows.Threading;
using WixToolset.BootstrapperApplicationApi;

namespace PictureTransformer.SetupUI;

public sealed class PictureTransformerBootstrapper : BootstrapperApplication
{
    private Dispatcher? dispatcher;
    private MainWindow? window;
    private IBootstrapperCommand? command;
    private bool installed;
    private bool applying;
    private int result;
    private LaunchAction plannedAction = LaunchAction.Unknown;

    protected override void OnCreate(CreateEventArgs args)
    {
        base.OnCreate(args);
        command = args.Command;
    }

    protected override void Run()
    {
        dispatcher = Dispatcher.CurrentDispatcher;

        DetectBegin += (_, e) => installed = e.RegistrationType == RegistrationType.Full;
        DetectComplete += OnDetectComplete;
        PlanComplete += OnPlanComplete;
        ApplyComplete += OnApplyComplete;
        Progress += (_, e) => Dispatch(() => window?.SetProgress(e.ProgressPercentage));
        Error += (_, e) =>
        {
            engine.Log(LogLevel.Error, e.ErrorMessage);
            e.Result = Result.Ok;
        };

        var defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PictureTransformer");
        var installPath = GetStringVariable("InstallFolder", defaultPath);
        var addToPath = GetNumericVariable("AddToPath", 1) != 0;
        engine.SetVariableString("InstallFolder", installPath, false);
        engine.SetVariableNumeric("AddToPath", addToPath ? 1 : 0);

        if (command?.Display is Display.Full or Display.Passive)
        {
            window = new MainWindow(this, installPath, addToPath);
            window.Closing += (_, e) =>
            {
                if (applying)
                {
                    e.Cancel = true;
                    return;
                }

                dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
            };
            window.Show();
        }

        engine.Detect();
        Dispatcher.Run();
        engine.Quit(NormalizeExitCode(result));
    }

    public void BeginInstall(string installPath, bool addToPath)
    {
        engine.SetVariableString("InstallFolder", Path.GetFullPath(installPath), false);
        engine.SetVariableNumeric("AddToPath", addToPath ? 1 : 0);
        BeginPlan(LaunchAction.Install, "正在安装");
    }

    public void BeginRepair() => BeginPlan(LaunchAction.Repair, "正在修复");

    public void BeginUninstall() => BeginPlan(LaunchAction.Uninstall, "正在卸载");

    private void BeginPlan(LaunchAction action, string progressHeader)
    {
        plannedAction = action;
        Dispatch(() => window?.ShowProgress(progressHeader));
        engine.Plan(action);
    }

    private void OnDetectComplete(object? sender, DetectCompleteEventArgs e)
    {
        if (e.Status < 0)
        {
            Finish(e.Status, false, "无法检查当前安装状态。请查看安装日志后重试。");
            return;
        }

        if (command?.Display == Display.Full)
        {
            if (command.Action == LaunchAction.Uninstall)
            {
                BeginUninstall();
            }
            else
            {
                Dispatch(() => window?.ShowReady(installed));
            }

            return;
        }

        var action = command?.Action ?? LaunchAction.Install;
        BeginPlan(action == LaunchAction.Unknown ? LaunchAction.Install : action, GetProgressHeader(action));
    }

    private void OnPlanComplete(object? sender, PlanCompleteEventArgs e)
    {
        if (e.Status < 0)
        {
            Finish(e.Status, false, "无法准备安装操作。请查看安装日志后重试。");
            return;
        }

        applying = true;
        var handle = window is null ? IntPtr.Zero : new WindowInteropHelper(window).Handle;
        engine.Apply(handle);
    }

    private void OnApplyComplete(object? sender, ApplyCompleteEventArgs e)
    {
        applying = false;
        var succeeded = e.Status >= 0;
        var message = succeeded
            ? plannedAction switch
            {
                LaunchAction.Uninstall => "PictureTransformer 已成功卸载。",
                LaunchAction.Repair => "PictureTransformer 已成功修复。",
                _ => "PictureTransformer 已成功安装。"
            }
            : "操作未完成。请查看安装日志后重试。";

        Finish(e.Status, succeeded, message);
    }

    private void Finish(int status, bool succeeded, string message)
    {
        result = status;
        if (command?.Display == Display.Full && window is not null)
        {
            Dispatch(() => window.ShowResult(succeeded, message));
        }
        else
        {
            dispatcher?.BeginInvokeShutdown(DispatcherPriority.Normal);
        }
    }

    private string GetStringVariable(string name, string fallback)
    {
        try
        {
            var value = engine.GetVariableString(name);
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
        catch
        {
            return fallback;
        }
    }

    private long GetNumericVariable(string name, long fallback)
    {
        try
        {
            return engine.GetVariableNumeric(name);
        }
        catch
        {
            return fallback;
        }
    }

    private void Dispatch(Action action)
    {
        if (dispatcher is null)
        {
            return;
        }

        if (dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            dispatcher.BeginInvoke(action);
        }
    }

    private static string GetProgressHeader(LaunchAction action) => action switch
    {
        LaunchAction.Uninstall => "正在卸载",
        LaunchAction.Repair => "正在修复",
        _ => "正在安装"
    };

    private static int NormalizeExitCode(int exitCode)
    {
        return (exitCode & unchecked((int)0xFFFF0000)) == unchecked((int)0x80070000)
            ? exitCode & 0xFFFF
            : exitCode;
    }
}
