using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;


namespace NeocitiesTests
{
    [TestFixture]
    public class Tests : IDisposable
    {
        private ChromeDriver driver;
        
        private const string BaseUrl = "https://neocities.org/";
        private bool _disposed = false;
        
        
        private IWebElement WaitForElement(By locator, int timeout = 20)
        {
            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeout));
                wait.IgnoreExceptionTypes(typeof(NoSuchElementException));

                return wait.Until(drv =>
                {
                    var element = drv.FindElement(locator);
                    return (element.Displayed && element.Enabled) ? element : null;
                });
            }
            catch (WebDriverException ex)
            {
                // Делаем скриншот с понятным именем
                var testName = TestContext.CurrentContext.Test.Name;
                var screenshot = ((ITakesScreenshot)driver).GetScreenshot();
                var fileName = $"{testName}_error_{DateTime.Now:yyyyMMddHHmmss}.png";
                screenshot.SaveAsFile(fileName);

                throw new AssertionException($"Element not found: {locator}\nTest: {testName}\nScreenshot: {fileName}", ex);
            }
        }

        [SetUp]
        public void Setup()
        {
            var options = new ChromeOptions();
            options.AddArguments(new List<string>
            {
                "--start-maximized",
                "--disable-notifications",
                "--lang=en",
                "--disable-popup-blocking"
            });
            
            driver = new ChromeDriver(options);
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2); // Уменьшаем implicit wait
            
            // Переход на сайт с обработкой возможного cookie-баннера
            driver.Navigate().GoToUrl(BaseUrl);
            
            try
            {
                var cookieBanner = driver.FindElement(By.CssSelector(".cookie-banner, .gdpr-modal"));
                var acceptButton = cookieBanner.FindElement(By.CssSelector("button.accept, .btn-primary"));
                acceptButton.Click();
                Thread.Sleep(500); // Даем время скрыться баннеру
            }
            catch (NoSuchElementException)
            {
                // Баннера нет - продолжаем
            }
        }

        [TearDown]
        public void TearDown()
        {
            driver?.Quit();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    driver?.Quit();
                    driver?.Dispose();
                }
                _disposed = true;
            }
        }

        ~Tests()
        {
            Dispose(false);
        }

        [Test]
        public void Test1_CheckPageTitle()
        {
            Assert.That(driver.Title, Is.EqualTo("Neocities: Create your own free website!"));
        }

        [Test]
        public void Test2_CheckMainElementsVisibility()
        {
            var logo = WaitForElement(By.CssSelector(".header-Intro .logo img"));
            var signUpButton = WaitForElement(By.CssSelector(".btn-Action.float-Right"));
            var loginButton = WaitForElement(By.CssSelector("a.sign-In"));
            
            Assert.Multiple(() =>
            {
                Assert.That(logo.Displayed, "Логотип не отображается");
                Assert.That(signUpButton.Displayed, "Кнопка регистрации не отображается");
                Assert.That(loginButton.Displayed, "Кнопка входа не отображается");
            });
        }

        [Test]
        public void Test3_SearchSites()
        {
            driver.Navigate().GoToUrl(BaseUrl + "browse");
            
            var searchInput = WaitForElement(By.CssSelector("input.search-input, input#tag"));
            searchInput.Clear();
            searchInput.SendKeys("portfolio");
            searchInput.SendKeys(Keys.Enter);
            
            WaitForElement(By.CssSelector(".website-Gallery, .search-results, .no-results"));
            
            Assert.That(driver.Url.ToLower().Contains("portfolio"), 
                "Результаты поиска не отобразились");
        }

        [Test]
        public void Test4_OpenSignUpForm()
        {
            var signUpButton = WaitForElement(By.CssSelector(".btn-Action.float-Right"));
            signUpButton.Click();
            
            var form = WaitForElement(By.CssSelector("fieldset.content"));
            Assert.That(form.Displayed, "Форма регистрации не отображается");
        }

        [Test]
        public void Test5_InvalidSignUpAttempt()
        {
            Test4_OpenSignUpForm();

            // Explicitly wait for the username input to be present and interactable
            var usernameInput = WaitForElement(By.CssSelector("input#username, input[name='username']")); // Added alternative selector for robustness
            usernameInput.SendKeys("test");
            
            var submitButton = WaitForElement(By.CssSelector("input[type='submit'], button[type='submit']")); // Added alternative selector for robustness
            submitButton.Click();
            
            // Ждем появления хотя бы одного сообщения об ошибке
            var errorMessages = WaitForElement(
                By.CssSelector(".tooltip-inner, .error-message, .alert.alert-danger"), // Added more common error message selectors
                15);
            
            Assert.That(errorMessages.Displayed, "Сообщение об ошибке не появилось");
        }
        [Test]
        public void Test6_OpenLoginForm()
        {
            var loginLink = WaitForElement(By.CssSelector("a.sign-In"));
            loginLink.Click();
            
            var loginForm = WaitForElement(By.CssSelector("form[action='/signin']"));
            Assert.That(loginForm.Displayed, "Форма входа не открылась");
        }

        [Test]
        public void Test7_InvalidLoginAttempt()
        {
            Test6_OpenLoginForm();

            var usernameInput = WaitForElement(By.CssSelector("input[name='username']"));
            usernameInput.SendKeys("invaliduser");
            
            var passwordInput = WaitForElement(By.CssSelector("input[name='password']"));
            passwordInput.SendKeys("wrongpassword");
            
            var submitButton = WaitForElement(By.CssSelector("button[type='submit'], input[type='submit']"));
            submitButton.Click();
            
            var errorMessage = WaitForElement(
                By.CssSelector(".error, .error-message, .login-error, .alert"), 
                15);
            
            Assert.That(errorMessage.Displayed, "Сообщение об ошибке не появилось");
        }

        [Test]
        public void Test8_NavigateToBrowsePage()
        {
            var browseLink = WaitForElement(By.CssSelector("a[href='/browse']"));
            browseLink.Click();
            
            WaitForElement(By.CssSelector(".website-Gallery"));
            Assert.That(driver.Url.Contains("/browse"), "Страница просмотра сайтов не загрузилась");
        }

        [Test]
        public void Test9_FilterSitesByTag()
        {
            driver.Navigate().GoToUrl(BaseUrl + "browse");

            var artTag = WaitForElement(By.CssSelector(".website-Gallery a[href*='tag=art']"));
            artTag.Click();
            
            WaitForElement(By.CssSelector(".website-Gallery"));
            Assert.That(driver.Url.Contains("tag=art"), "Фильтр по тегу не применился");
        }

        [Test]
        public void Test10_ViewSiteDetails()
        {
            driver.Navigate().GoToUrl(BaseUrl + "browse");

            // Измененный селектор для первого элемента галереи
            var firstSite = WaitForElement(
                By.CssSelector(".website-Gallery .gallery-item a:first-child, .website-Gallery a:first-child"));
            firstSite.Click();

            var siteTitle = WaitForElement(
                By.CssSelector(".website-title, h1, .site-header"),
                10);

            Assert.That(siteTitle.Displayed, "Заголовок сайта не отображается");
        }

        [Test]
        public void Test11_CheckTutorialsPage()
        {
            driver.Navigate().GoToUrl("https://neocities.org/tutorials");
            
            WaitForElement(By.CssSelector("body, main, .main-content"), 30);
            
            IWebElement pageTitle = null;
            var possibleSelectors = new List<string>
            {
                "h1",
                ".page-title",
                ".header h1",
                "main h1",
                ".content h1",
                ".tutorial-header h1",
                "h1.title"
            };
            
            foreach (var selector in possibleSelectors)
            {
                try
                {
                    pageTitle = driver.FindElement(By.CssSelector(selector));
                    if (pageTitle.Displayed && !string.IsNullOrEmpty(pageTitle.Text))
                        break;
                }
                catch (NoSuchElementException) { }
            }
            
            if (pageTitle == null)
            {
                var screenshot = ((ITakesScreenshot)driver).GetScreenshot();
                screenshot.SaveAsFile("tutorials_page_error.png");
                Assert.Fail("Не удалось найти заголовок на странице tutorials. Скриншот сохранен.");
            }
            
            Assert.Multiple(() =>
            {
                Assert.That(pageTitle.Text, Is.Not.Empty, "Заголовок страницы tutorials пустой");
                
                var titleLower = pageTitle.Text.ToLower();
                var expectedKeywords = new[] { "learn", "tutorial", "guide", "how to", "help", "getting started" };
                Assert.That(
                    expectedKeywords.Any(kw => titleLower.Contains(kw)),
                    $"Заголовок '{pageTitle.Text}' не содержит ожидаемых ключевых слов"
                );
                
                Assert.That(driver.Url.ToLower().Contains("tutorials"), "URL не соответствует странице tutorials");
                
                // ИСПРАВЛЕННЫЙ СЕЛЕКТОР
                var tutorials = driver.FindElements(By.CssSelector(".misc-page ul li a, a[href*='/tutorial/html/']")); 
                Assert.That(tutorials.Count, Is.GreaterThan(0), "На странице не найдено ни одного tutorial");
            });
        }
    }
}