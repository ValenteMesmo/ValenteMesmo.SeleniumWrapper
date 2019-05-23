﻿using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SeleniumWrapper4000
{
    public class SeleniumWrapper : IDisposable
    {
        private ChromeDriver driver;
        private int currentTimeoutInMilliseconds = 1000;

        public SeleniumWrapper(bool headless = true)
        {
            driver = CreateWebDriver(headless);
            SetImplicityWait(currentTimeoutInMilliseconds);
        }

        private ChromeDriver CreateWebDriver(bool headless = true)
        {
            var cService = ChromeDriverService.CreateDefaultService();
            cService.HideCommandPromptWindow = true;

            // Optional
            var options = new ChromeOptions();

            if (headless)
            {
                options.AddArgument("--headless");
                options.AddArgument("--disable-gpu");
            }
            else
            {
                options.AddArguments("disable-infobars");
                options.AddArguments("--start-maximized");
            }

            var webdriver = new ChromeDriver(cService, options);

            Process.GetProcessById(cService.ProcessId)
                .AttachToCurrentProcess();

            //create list of process id
            var driverProcessIds = new List<int> { cService.ProcessId };

            //Get all the childs generated by the driver like conhost, chrome.exe...
            var mos = new System.Management.ManagementObjectSearcher(
                $"Select * From Win32_Process Where ParentProcessID={cService.ProcessId}"
            );

            foreach (var mo in mos.Get())
                driverProcessIds.Add(Convert.ToInt32(mo["ProcessID"]));

            //Kill all
            foreach (var id in driverProcessIds)
                Process.GetProcessById(id)
                    .AttachToCurrentProcess();

            return webdriver;
        }

        public void GoToUrl(string url)
        {
            if (driver.Url != url)
                driver.Navigate().GoToUrl(url);
        }

        public void Refresh() => driver.Navigate().Refresh();

        public void Click(string selector)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromMilliseconds(currentTimeoutInMilliseconds));
            IWebElement element = null;
            wait.Until(drv =>
            {
                element = driver.FindElementByCssSelector(selector);
                return element != null
                    && element.Displayed
                    && element.Enabled;
            });

            element.Click();
        }

        public void WaitTextCondition(string selector, Func<string, bool> condition, int? milliseconds = null)
        {
            if (!milliseconds.HasValue)
                milliseconds = currentTimeoutInMilliseconds;

            var wait = new WebDriverWait(driver, TimeSpan.FromMilliseconds(milliseconds.Value));

            wait.Until(drv =>
            {
                IWebElement element = null;
                try
                {
                    element = driver.FindElementByCssSelector(selector);
                }
                catch { }
                if (element == null)
                    return false;
                if (!element.Displayed)
                    return false;
                if (element.Text == null)
                    return false;
                return condition(element.Text);
            });
        }

        public void PressEnter(string selector) => driver.FindElementByCssSelector(selector).SendKeys(Keys.Enter);

        public string GetText(string selector)
        {
            var element = driver.FindElementByCssSelector(selector);
            if (element.TagName == "select")
                return new SelectElement(element).SelectedOption?.Text;

            if (element.TagName == "textarea" || element.Text == "")
                return element.GetAttribute("value");

            return element.Text;
        }

        private IWebElement GetCell(string selector, int rowIndex, int columnIndex, int? milliseconds = null)
        {
            if (!milliseconds.HasValue)
                milliseconds = currentTimeoutInMilliseconds;

            IWebElement cell = null;
            var wait = new WebDriverWait(driver, TimeSpan.FromMilliseconds(milliseconds.Value));

            wait.Until(drv =>
            {
                try
                {
                    var table = driver.FindElementByCssSelector(selector);

                    var row = table.FindElements(By.TagName("tr"))[rowIndex];
                    cell = row.FindElements(By.TagName("td"))[columnIndex];

                }
                catch { }
                return cell != null;
            });

            return cell;
        }

        public string GetTextFromTableCell(string selector, int rowIndex, int columnIndex, int? milliseconds = null) =>
            GetCell(selector, rowIndex, columnIndex, milliseconds).Text;

        public void ClickOnTableCell(string tableSelector, int rowIndex, int columnIndex, string selector, int? milliseconds = null)
        {
            var cell = GetCell(tableSelector, rowIndex, columnIndex, milliseconds);

            var elements = cell.FindElements(By.CssSelector(selector));
            foreach (var element in elements)
            {
                if (element.Displayed)
                {
                    element.Click();
                    break;
                }
            }
        }

        public void WaitInTableCell(string tableSelector, int rowIndex, int columnIndex, string inCellSelector, int? milliseconds = null)
        {
            if (!milliseconds.HasValue)
                milliseconds = currentTimeoutInMilliseconds;

            var wait = new WebDriverWait(driver, TimeSpan.FromMilliseconds(milliseconds.Value));

            wait.Until(drv =>
            {
                var cell = GetCell(tableSelector, rowIndex, columnIndex, milliseconds);
                var element = cell.FindElement(By.CssSelector(inCellSelector));

                return element != null
                   && element.Displayed;
            });
        }

        public bool IsSelected(string selector)
        {
            var element = driver.FindElementByCssSelector(selector);
            if (element == null)
                throw new Exception($"Elemento nao encontrado: {selector}");

            return element.Selected;
        }

        public bool IsNotSelected(string selector)
        {
            var element = driver.FindElementByCssSelector(selector);
            if (element == null)
                throw new Exception($"Elemento nao encontrado: {selector}");

            return !element.Selected;
        }

        public bool IsVisible(string selector)
        {
            var element = driver.FindElementByCssSelector(selector);
            if (element == null)
                throw new Exception($"Elemento nao encontrado: {selector}");

            return element.Displayed;
        }

        public bool IsInvisible(string selector)
        {
            var element = driver.FindElementByCssSelector(selector);
            if (element == null)
                throw new Exception($"Elemento nao encontrado: {selector}");

            return !element.Displayed;
        }

        public bool IsDisabled(string selector)
        {
            var element = driver.FindElementByCssSelector(selector);
            if (element == null)
                throw new Exception($"Elemento nao encontrado: {selector}");

            return !element.Enabled;
        }

        public bool IsEnabled(string selector)
        {
            var element = driver.FindElementByCssSelector(selector);
            if (element == null)
                throw new Exception($"Elemento nao encontrado: {selector}");

            return element.Enabled;
        }

        public void WaitInvisibilityOf(string selector, int? milliseconds = null)
        {
            if (!milliseconds.HasValue)
                milliseconds = currentTimeoutInMilliseconds;

            var wait = new WebDriverWait(driver, TimeSpan.FromMilliseconds(milliseconds.Value));

            wait.Until(drv =>
            {
                var element = driver.FindElementByCssSelector(selector);
                return element == null
                    || !element.Displayed;
            });
        }

        public void WaitVisibilityOf(string selector, int? milliseconds = null)
        {
            if (!milliseconds.HasValue)
                milliseconds = currentTimeoutInMilliseconds;

            var wait = new WebDriverWait(driver, TimeSpan.FromMilliseconds(milliseconds.Value));

            wait.Until(drv =>
            {
                var element = driver.FindElementByCssSelector(selector);
                return element != null
                    && element.Displayed;
            });
        }

        public void MaximizeWindow() => driver.Manage().Window.Maximize();

        public void MinimizeWindow() => driver.Manage().Window.Minimize();

        public void SelectOption(string selector, string value)
        {
            var select = driver.FindElementByCssSelector(selector);
            var options = select.FindElements(By.TagName("option"));
            foreach (var option in options)
            {
                if (option.Text.Equals(value))
                {
                    option.Click();
                    break;
                }
            }
        }

        public void SetText(string selector, string text)
        {
            var element = driver.FindElementByCssSelector(selector);
            element.Clear();
            element.SendKeys(text);
        }

        public void SetImplicityWait(int milliseconds)
        {
            currentTimeoutInMilliseconds = milliseconds;
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(currentTimeoutInMilliseconds);
        }

        public void Close() => driver.Quit();

        public void Dispose() => driver.Quit();

        public void SetFile(string selector, string fileName, string fileContent)
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

            File.WriteAllText(filePath, fileContent);

            var element = driver.FindElementByCssSelector(selector);
            element.SendKeys(filePath);
        }

        public void SetFile(string selector)
        {
            var value = Guid.NewGuid();
            SetFile(selector, $"Teste_{value}.txt", value.ToString());
        }

        public void SetFile(string selector, string filePath)
        {
            var element = driver.FindElementByCssSelector(selector);
            element.SendKeys(filePath);
        }

        public T ExecuteScript<T>(string script, params object[] args)
        {
            return (T)driver.ExecuteScript(script, args);
        }

        public object ExecuteScript(string script, params object[] args)
        {
            return driver.ExecuteScript(script, args);
        }
    }
}
