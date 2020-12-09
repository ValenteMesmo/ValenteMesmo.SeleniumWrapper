﻿using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Remote;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;

namespace ValenteMesmo.SeleniumWrapper
{
    public class SeleniumWrapper : IDisposable
    {
        internal readonly RemoteWebDriver driver;

        internal int currentTimeoutInMilliseconds;

        public SeleniumWrapper(bool headless = false, int implicityWait = 1000, BrowserType BrowserType = BrowserType.Chrome)
        {
            currentTimeoutInMilliseconds = implicityWait;

            if (BrowserType == BrowserType.Chrome)
                driver = CreateChromeDriver(headless);
            else
                driver = CreateIEDriver();

            this.SetImplicityWait(currentTimeoutInMilliseconds);
        }

        private static RemoteWebDriver CreateIEDriver()
        {
            var cService = InternetExplorerDriverService.CreateDefaultService();
            cService.HideCommandPromptWindow = true;
            cService.SuppressInitialDiagnosticInformation = true;

            InternetExplorerOptions options = new InternetExplorerOptions();
            options.IntroduceInstabilityByIgnoringProtectedModeSettings = true;
            options.IgnoreZoomLevel = true;

            var webdriver = new InternetExplorerDriver(cService, options);

            AttachToCurrentProcess(cService.ProcessId);

            return webdriver;
        }

        private static RemoteWebDriver CreateChromeDriver(bool headless)
        {
            var cService = ChromeDriverService.CreateDefaultService();
            cService.HideCommandPromptWindow = true;
            cService.SuppressInitialDiagnosticInformation = true;

            var options = new ChromeOptions();
            if (headless)
            {
                options.AddArguments("--headless");
                options.AddArguments("--disable-gpu");
            }
            else
            {
                options.AddArguments("disable-infobars");
                options.AddArguments("--start-maximized");
            }

            options.AddArguments("fast-start");

            options.AddArguments("--incognito");
            options.AddArguments("--no-sandbox");

            try
            {
                var webdriver = new ChromeDriver(cService, options);

                AttachToCurrentProcess(cService.ProcessId);

                return webdriver;
            }
            catch (InvalidOperationException exception)
            {
                var pattern = @"Chrome version (\d{2,})";
                var match = Regex.Match(exception.Message, pattern);

                if (match.Success && match.Groups.Count == 2)
                    throw new InvalidOperationException(
                        $@"The webdriver version does not match browser version...
Google 'download chrome webdriver' and download version {match.Groups[1]}..."

                        , exception);

                throw exception;
            }
        }

        private static void AttachToCurrentProcess(int ProcessId)
        {
            var driverProcessIds = new List<int> { ProcessId };

            //Get all the childs generated by the driver like conhost, chrome.exe...
            using (var mos = new ManagementObjectSearcher(
                $"Select * From Win32_Process Where ParentProcessID={ProcessId}"
            ))
                foreach (var mo in mos.Get())
                    driverProcessIds.Add(Convert.ToInt32(mo["ProcessID"]));

            //TODO: receive packages folder as optional parameter.
            Process process = new Process();
            //process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            var projectDir = Environment.CurrentDirectory.Split(new[] { "\\bin" }, StringSplitOptions.RemoveEmptyEntries)[0];
            var solutionDir = Directory.GetParent(projectDir).FullName;
            var fsharpCompilerDir = Directory.GetDirectories(Path.Combine(solutionDir, "packages"))
                .OrderByDescending(f => f)
                .FirstOrDefault(f => f.Contains("FSharp.Compiler.Tools."));

            process.StartInfo.FileName = $@"{fsharpCompilerDir}\tools\fsi.exe";
            process.StartInfo.Arguments = $"KillWithParent.fsx {Process.GetCurrentProcess().Id} {string.Join(" ", driverProcessIds)}";
            process.Start();
        }

        public void Dispose()
        {
            driver.Dispose();
        }
    }
}