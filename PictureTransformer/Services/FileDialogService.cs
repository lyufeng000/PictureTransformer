using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Win32;

namespace PictureTransformer.Services;

public interface IFileDialogService
{
    IReadOnlyList<string> SelectImageFiles();
    string? SelectInputFolder();
    string? SelectOutputFolder();
}

public sealed class FileDialogService : IFileDialogService
{
    public IReadOnlyList<string> SelectImageFiles()
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择图片",
            Filter = "图片文件|*.jpg;*.jpeg;*.jpe;*.png;*.apng;*.webp;*.avif;*.heic;*.heif;*.tif;*.tiff;*.bmp;*.dib;*.gif;*.ico;*.tga;*.pcx;*.jp2;*.j2k;*.dds;*.exr;*.hdr;*.psd;*.qoi;*.ppm;*.pgm;*.pbm;*.pam;*.pnm|所有文件|*.*",
            Multiselect = true,
            InitialDirectory = KnownFolders.Downloads
        };
        return dialog.ShowDialog() == true ? dialog.FileNames : [];
    }

    public string? SelectInputFolder() => SelectFolder("选择包含图片的文件夹");
    public string? SelectOutputFolder() => SelectFolder("选择输出目录");

    private static string? SelectFolder(string title)
    {
        var dialog = new OpenFolderDialog { Title = title, InitialDirectory = KnownFolders.Downloads, Multiselect = false };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}

internal static class KnownFolders
{
    private static readonly Guid DownloadsFolderId = new("374DE290-123F-4565-9164-39C4925E467B");

    public static string Downloads
    {
        get
        {
            IntPtr pathPointer = IntPtr.Zero;
            try
            {
                if (SHGetKnownFolderPath(DownloadsFolderId, 0, IntPtr.Zero, out pathPointer) == 0)
                    return Marshal.PtrToStringUni(pathPointer)!;
            }
            finally
            {
                if (pathPointer != IntPtr.Zero) Marshal.FreeCoTaskMem(pathPointer);
            }
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }
    }

    [DllImport("shell32.dll")]
    private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint flags, IntPtr token, out IntPtr path);
}
