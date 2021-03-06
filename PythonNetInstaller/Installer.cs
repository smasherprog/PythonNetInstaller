﻿using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace PythonNetInstaller
{
    public static class Installer
    {
        private static string EMBEDDED_PYTHON = string.Empty;
        private static string INSTALL_PATH { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private static string EnvPath = string.Empty;
        public static string PythonHomeDirectory
        {
            get
            {
                var install_dir = Path.Combine(INSTALL_PATH, EMBEDDED_PYTHON);
                return install_dir;
            }
        }

        public static Action<string> OutputDataReceived { get; set; } = (d) => { Console.WriteLine(d); };
        public static Action<string> ErrorDataReceived { get; set; } = (d) => { Console.WriteLine(d); };

        public static void InstallPythonFromUri(Uri download_url)
        {
            var stringurl = download_url.ToString();
            if (!stringurl.EndsWith(".zip"))
            {
                throw new ArgumentException("Must end in .zip", "download_url");
            }

            var filepath = Path.GetFileName(stringurl);
            InternalRunCommand($"cd {INSTALL_PATH} && curl {stringurl} -o {filepath}");
            var extractpath = Path.Combine(INSTALL_PATH, filepath);
            InstallPythonFromZip(extractpath);
        }

        private static void RemoveFromPathWhereFileExists(string filename)
        {
            var path = Environment.GetEnvironmentVariable("PATH");
            var directories = path.Split(';');
            var newenv = string.Empty;
            foreach (var dir in directories)
            {
                var fullpath = Path.Combine(dir, filename);
                if (!File.Exists(fullpath))
                {
                    newenv += dir + ";";
                }
            }
            Environment.SetEnvironmentVariable("PATH", newenv);
        }

        public static void InstallPythonFromZip(string file_path)
        {
            if (!file_path.EndsWith(".zip"))
            {
                throw new ArgumentException("Must end in .zip", "file_path");
            }

            var resource_name = Path.GetFileName(file_path).Replace(".zip", "");
            var extractpath = Path.Combine(INSTALL_PATH, resource_name);
            if (Directory.Exists(extractpath))
            {
                Directory.Delete(extractpath, true);
            }
            ZipFile.ExtractToDirectory(file_path, extractpath);
            EMBEDDED_PYTHON = resource_name;
            RemoveFromPathWhereFileExists("python.exe");
            RemoveFromPathWhereFileExists("pip.exe");
            Environment.SetEnvironmentVariable("PATH", $"{PythonHomeDirectory};" + Environment.GetEnvironmentVariable("PATH"));
            var scriptsdir = Path.Combine(PythonHomeDirectory, "Scripts");
            if (!Directory.Exists(scriptsdir))
            {
                Directory.CreateDirectory(scriptsdir);
            }
            Environment.SetEnvironmentVariable("PATH", $"{scriptsdir};" + Environment.GetEnvironmentVariable("PATH"));
            EnvPath = Environment.GetEnvironmentVariable("PATH");
            var file = Directory.GetFiles(PythonHomeDirectory, "*._pth", SearchOption.TopDirectoryOnly);
            var filecontents = File.ReadAllText(file[0]).Replace("#import site", "import site");
            File.WriteAllText(file[0], filecontents);
        }

        public static void InstallPythonFromAssembly(Assembly assembly, string resource_name)
        {
            if (!resource_name.EndsWith(".zip"))
            {
                throw new ArgumentException("Must end in .zip", "resource_name");
            }

            var extractpath = Path.Combine(INSTALL_PATH, resource_name);
            CopyEmbeddedResourceToFile(assembly, resource_name, extractpath);
            InstallPythonFromZip(extractpath);
        }

        public static void InstallPip()
        {
            string libDir = Path.Combine(PythonHomeDirectory, "Lib");
            if (!Directory.Exists(libDir))
            {
                Directory.CreateDirectory(libDir);
            }
            InternalRunCommand($"cd {libDir} && curl https://bootstrap.pypa.io/get-pip.py -o get-pip.py");
            InternalRunCommand($"cd {PythonHomeDirectory} && python.exe Lib\\get-pip.py");
        }

        private static void CopyEmbeddedResourceToFile(Assembly assembly, string resourceName, string filePath)
        {
            var key = assembly.GetManifestResourceNames().FirstOrDefault(x => x.Contains(resourceName));
            using (Stream stream = assembly.GetManifestResourceStream(key))
            using (var file = new FileStream(filePath, FileMode.Create))
            {
                if (stream == null)
                    throw new ArgumentException($"Resource name '{resourceName}' not found!");
                stream.CopyTo(file);
            }
        }

        public static bool IsPythonInstalled()
        {
            return File.Exists(Path.Combine(PythonHomeDirectory, "python.exe"));
        }

        public static bool IsPipInstalled()
        {
            return File.Exists(Path.Combine(PythonHomeDirectory, "Scripts", "pip.exe"));
        }

        public static bool IsModuleInstalled(string module)
        {
            if (!IsPythonInstalled())
                return false;

            string moduleDir = Path.Combine(PythonHomeDirectory, "Lib", module);
            return Directory.Exists(moduleDir) && File.Exists(Path.Combine(moduleDir, "__init__.py"));
        }

        private static void InternalRunCommand(string command)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            string commandMode = "/C";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                process.StartInfo.FileName = "/bin/bash";
                commandMode = "-c";
            }
            process.StartInfo.Arguments = $"{commandMode} {command}";
            process.OutputDataReceived += (s, e) => OutputDataReceived?.Invoke(e.Data);
            process.ErrorDataReceived += (s, e) => ErrorDataReceived?.Invoke(e.Data);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }

        public static void RunCommand(string command)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.StartInfo.EnvironmentVariables["Path"] = EnvPath;
            string commandMode = "/C";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                process.StartInfo.FileName = "/bin/bash";
                commandMode = "-c";
            }
            process.StartInfo.Arguments = $"{commandMode} {command}";
            process.OutputDataReceived += (s, e) => OutputDataReceived?.Invoke(e.Data);
            process.ErrorDataReceived += (s, e) => ErrorDataReceived?.Invoke(e.Data);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }
    }
}
