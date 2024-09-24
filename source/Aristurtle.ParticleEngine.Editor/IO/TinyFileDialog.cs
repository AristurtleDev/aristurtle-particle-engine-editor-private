// Copyright (c) Christopher Whitley. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Aristurtle.ParticleEngine.Editor.IO;

public static partial class TinyFileDialog
{

#if LINUX
    private const string LibraryName = "runtimes/linux-x64/native/tinyfiledialogs.so";
#elif MAC
    private const string LibraryName = "runtimes/osx-64/native/tinyfiledialogs.dylib";
#else
    private const string LibraryName = "runtimes/win-x64/native/tinyfiledialogs.dll";
#endif

    [LibraryImport(LibraryName, EntryPoint = "tinyfd_saveFileDialog", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr SaveFileDialogNative(string aTitle, string aDefaultPathAndFile, int aNumOfFilterPatterns, string[] aFilterPatterns, string aSingleFilterDescription);

    [LibraryImport(LibraryName, EntryPoint = "tinyfd_openFileDialog", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr OpenFileDialogNative(string aTitle, string aDefaultPathAndFile, int aNumOfFilterPatterns, string[] aFilterPatterns, string aSingleFilterDescription, int aAllowMultipleSelects);

    [LibraryImport(LibraryName, EntryPoint = "tinyfd_selectFolderDialog", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr SelectFolderDialogNative(string aTitle, string aDefaultPathAndFile);

    [LibraryImport(LibraryName, EntryPoint = "tinyfd_messageBox", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int MessageBoxNative(string aTitle, string aMessage, string aDialogType, string aIconType, int aDefaultButton);

    public static string SaveFile(string title, string defaultPath, string filters, string filterDescription)
    {
        string[] filterPatterns = filters.Split(',', StringSplitOptions.RemoveEmptyEntries);
        IntPtr result = SaveFileDialogNative(title, defaultPath, filterPatterns.Length, filterPatterns, filterDescription);
        return Marshal.PtrToStringAnsi(result);
    }

    public static string OpenFile(string title, string defaultPath, string filters, string filterDescription, bool allowMultiSelect)
    {
        string[] filterPatterns = filters.Split(',', StringSplitOptions.RemoveEmptyEntries);
        IntPtr result = OpenFileDialogNative(title, defaultPath, filterPatterns.Length, filterPatterns, filterDescription, allowMultiSelect ? 1 : 0);
        return Marshal.PtrToStringAnsi(result);
    }

    public static string SelectFolder(string title, string defaultPath)
    {
        IntPtr result = SelectFolderDialogNative(title, defaultPath);
        return Marshal.PtrToStringAnsi(result);
    }

    public static string MessageBox(string title, string message, string dialogType, string iconType, int defaultButton)
    {
        IntPtr result = MessageBoxNative(title, message, dialogType, iconType, defaultButton);
        return Marshal.PtrToStringAnsi(result);
    }

    public static class DialogType
    {
        public const string OK = "ok";
        public const string OK_CANCEL = "okcancel";
        public const string YES_NO = "yesno";
        public const string YES_NO_CANCEL = "yesnocancel";
    }

    public static class IconType
    {
        public const string INFO = "info";
        public const string WARNING = "warning";
        public const string ERROR = "error";
        public const string QUESTION = "question";
    }
}
