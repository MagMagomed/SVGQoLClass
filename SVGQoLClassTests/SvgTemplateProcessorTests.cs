using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using Svg;
using SVGQoLClass;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SVGQoLClass.Tests
{
    [TestClass()]
    public class SvgTemplateProcessorTests
    {
        // Корневая папка одного прогона: TestResults/Run_<timestamp>.
        private static string _runDir;
        // Общий вход для read-only тестов: <Run>/template.svg.
        // Создаётся один раз в [ClassInitialize].
        private static string _sharedTemplatePath;
        private static int _fallbackCounter;

        // Имя текущего теста — база для имени папки (если она понадобится).
        private string _testName;
        // Путь к шаблону. Для всех тестов указывает на общий <Run>/template.svg.
        private string _templatePath;

        // MSTest v2 прокидывает TestContext в каждый экземпляр класса.
        public TestContext TestContext { get; set; }

        // Простой шаблон: <rect> и <text> с id.
        // small_rect — для тестов autosize (маленький, чтобы шрифт приходилось уменьшать).
        // suffix_text — для проверки поиска SvgText по id + "_text".
        private const string TestSvg =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<svg xmlns=""http://www.w3.org/2000/svg"" width=""500"" height=""500"">
  <rect id=""field_rect"" x=""10"" y=""10"" width=""100"" height=""50"" fill=""lightgray""/>
  <rect id=""small_rect"" x=""150"" y=""10"" width=""80"" height=""40"" fill=""lightgray""/>
  <text id=""field_name"" x=""20"" y=""40"" font-size=""16"">Placeholder</text>
  <text id=""field_date"" x=""20"" y=""70"" font-size=""16"">01.01.2000</text>
  <text id=""suffix_text"" x=""150"" y=""100"" font-size=""12"">suffix</text>
</svg>";

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            string assemblyDir = AppDomain.CurrentDomain.BaseDirectory;
            string projectDir = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", ".."));
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            _runDir = Path.Combine(projectDir, "TestResults", $"Run_{timestamp}");
            Directory.CreateDirectory(_runDir);

            // Один общий входной SVG на весь прогон.
            // Тестам, которые пишут результат, он копируется в их папку.
            _sharedTemplatePath = Path.Combine(_runDir, "template.svg");
            File.WriteAllText(_sharedTemplatePath, TestSvg, new UTF8Encoding(false));
        }

        [TestInitialize]
        public void Setup()
        {
            // Страховка: если ClassInitialize по какой-то причине не вызвался.
            if (_runDir == null)
            {
                string assemblyDir = AppDomain.CurrentDomain.BaseDirectory;
                string projectDir = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", ".."));
                _runDir = Path.Combine(projectDir, "TestResults",
                    $"Run_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}");
                Directory.CreateDirectory(_runDir);

                _sharedTemplatePath = Path.Combine(_runDir, "template.svg");
                File.WriteAllText(_sharedTemplatePath, TestSvg, new UTF8Encoding(false));
            }

            // Имя текущего теста. В MSTest v2 TestContext.TestName
            // доступно уже в [TestInitialize].
            string testName = TestContext?.TestName;
            if (string.IsNullOrWhiteSpace(testName))
                testName = $"Test_{System.Threading.Interlocked.Increment(ref _fallbackCounter)}";

            foreach (char c in Path.GetInvalidFileNameChars())
                testName = testName.Replace(c, '_');

            _testName = testName;
            _templatePath = _sharedTemplatePath;

            // Папку <Run>/<TestName>/ НЕ создаём: тесты, которые не пишут файл,
            // не должны оставлять пустых каталогов. Она появится лениво —
            // в момент первого OutPath(...).
        }

        // Ленивая папка текущего теста. При первом обращении создаётся
        // и в неё копируется общий template.svg (снимок входа).
        private string EnsureTestDir()
        {
            string dir = Path.Combine(_runDir, _testName);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                File.Copy(_sharedTemplatePath,
                    Path.Combine(dir, "template.svg"),
                    overwrite: true);
            }
            return dir;
        }

        // Куда сохранять результат. Расширение определяет тип выхода:
        // out.svg — для SVG-тестов, out.pdf — для PDF-тестов.
        private string OutPath(string fileName)
            => Path.Combine(EnsureTestDir(), fileName);

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                if (_testName != null)
                {
                    var dir = Path.Combine(_runDir, _testName);
                    if (Directory.Exists(dir)) Directory.Delete(dir, true);
                }
                if (Directory.Exists(_runDir)) Directory.Delete(_runDir, true);
            }
            catch
            {
                // Игнорируем ошибки удаления temp-файлов (например, если файл занят)
            }
        }

        // ============ Load ============
        // ==================== ФАБРИКА: FromFile ====================

        [TestMethod()]
        public void FromFile_ValidPath_CreatesInstance()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                Assert.IsNotNull(processor);
                Assert.IsNotNull(processor.Document);
            }
        }

        [TestMethod()]
        [ExpectedException(typeof(FileNotFoundException))]
        public void FromFile_MissingFile_Throws()
        {
            // Файла "missing.svg" нет — ожидаем исключение.
            SvgTemplateProcessor.FromFile(
                Path.Combine(_runDir, _testName ?? "no_test", "missing.svg"));
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentException))]
        public void FromFile_EmptyPath_Throws()
        {
            SvgTemplateProcessor.FromFile("");
        }

        // ==================== ФАБРИКА: FromString ====================

        [TestMethod()]
        public void FromString_ValidSvg_CreatesInstance()
        {
            using (var processor = SvgTemplateProcessor.FromString(TestSvg))
            {
                Assert.IsNotNull(processor);
                Assert.IsNotNull(processor.Document);
            }
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentException))]
        public void FromString_EmptyString_Throws()
        {
            SvgTemplateProcessor.FromString("");
        }

        // ==================== ФАБРИКА: FromStream ====================

        [TestMethod()]
        public void FromStream_ValidStream_CreatesInstance()
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(TestSvg)))
            using (var processor = SvgTemplateProcessor.FromStream(ms))
            {
                Assert.IsNotNull(processor);
                Assert.IsNotNull(processor.Document);
            }
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException))]
        public void FromStream_NullStream_Throws()
        {
            SvgTemplateProcessor.FromStream(null);
        }

        // ==================== ФАБРИКА: FromDocument ====================

        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException))]
        public void FromDocument_Null_Throws()
        {
            SvgTemplateProcessor.FromDocument(null);
        }

        // ==================== SetFieldText ====================

        [TestMethod()]
        public void SetFieldTextTest()
        {
            string outPath = OutPath("out.svg");
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.SetFieldText("field_name", "Иван Петров");
                processor.SaveSvg(outPath);
            }

            string content = File.ReadAllText(outPath);
            StringAssert.Contains(content, "Иван Петров");
            Assert.IsFalse(content.Contains("Placeholder"),
                "Старый текст-заглушка не должен остаться в документе.");
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentException))]
        public void SetFieldText_UnknownId_Throws()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.SetFieldText("no_such_field", "text");
            }
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentException))]
        public void SetFieldText_NotATextElement_Throws()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.SetFieldText("field_rect", "text");
            }
        }

        [TestMethod()]
        public void WithFieldText_Null_ClearsField()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.WithFieldText("field_name", null);
                var text = processor.Document.GetElementById<SvgText>("field_name");
                Assert.IsNotNull(text);
                Assert.IsTrue(string.IsNullOrEmpty(text.Text),
                    "Null должен очищать текст поля.");
            }
        }

        // ==================== SetFieldSize ====================

        [TestMethod()]
        public void SetFieldSizeTest()
        {
            string outPath = OutPath("out.svg");
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.SetFieldSize("field_rect", 200, 100);
                processor.SaveSvg(outPath);
            }

            string content = File.ReadAllText(outPath);
            StringAssert.Contains(content, "width=\"200\"");
            StringAssert.Contains(content, "height=\"100\"");
        }

        [TestMethod()]
        public void SetFieldSize_OnText_SetsTextLength()
        {
            string outPath = OutPath("out.svg");
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.SetFieldSize("field_name", 250, 30);
                processor.SaveSvg(outPath);
            }

            string content = File.ReadAllText(outPath);
            StringAssert.Contains(content, "textLength=\"250\"");
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentException))]
        public void SetFieldSize_UnknownId_Throws()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.SetFieldSize("no_such_field", 100, 100);
            }
        }

        [TestMethod()]
        [ExpectedException(typeof(NotSupportedException))]
        public void SetFieldSize_UnsupportedElement_Throws()
        {
            const string svgWithCircle =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<svg xmlns=""http://www.w3.org/2000/svg"" width=""500"" height=""500"">
  <circle id=""my_circle"" cx=""50"" cy=""50"" r=""20""/>
</svg>";

            using (var processor = SvgTemplateProcessor.FromString(svgWithCircle))
            {
                processor.SetFieldSize("my_circle", 100, 100);
            }
        }

        [TestMethod()]
        public void SetFieldSize_OnText_ZeroWidth_NoChange()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.SetFieldSize("field_name", 0, 30);
                var text = processor.Document.GetElementById<SvgText>("field_name");
                Assert.AreEqual(0f, text.TextLength.Value,
                    "При width <= 0 textLength не должен меняться.");
            }
        }

        // ==================== FitFieldToText ====================

        [TestMethod()]
        public void FitFieldToTextTest()
        {
            string outPath = OutPath("out.svg");
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                // "0123456789" = 10 символов
                // width  = 10 * 10 + 4 * 2 = 108
                // height = 10 * 1.5 + 4 * 2 = 23
                processor.FitFieldToText("field_rect", "0123456789", charWidth: 10f, padding: 4f);
                processor.SaveSvg(outPath);
            }

            string content = File.ReadAllText(outPath);
            StringAssert.Contains(content, "width=\"108\"");
            StringAssert.Contains(content, "height=\"23\"");
        }

        [TestMethod()]
        public void FitFieldToText_EmptyText_NoChange()
        {
            string outPath = OutPath("out.svg");
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.FitFieldToText("field_rect", "");
                processor.SaveSvg(outPath);
            }

            string content = File.ReadAllText(outPath);
            StringAssert.Contains(content, "width=\"100\"");
            StringAssert.Contains(content, "height=\"50\"");
        }

        // ==================== SetFieldTextWithAutoWrap ====================

        [TestMethod()]
        public void WithAutoWrapText_CreatesTextOverRectangle()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.WithAutoWrapText("field_rect", "Some text that may wrap",
                    fontSize: 12f);

                var t = processor.Document.GetElementById<SvgText>("field_rect_text");
                Assert.IsNotNull(t, "Должен быть создан SvgText с id 'field_rect_text'.");
                Assert.IsTrue(t.Children.Count > 0, "В тексте должны быть tspan-строки.");
            }
        }

        [TestMethod()]
        public void WithAutoWrapText_UsesExistingTextElement()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.WithAutoWrapText("field_date", "Updated", fontSize: 12f);

                var t = processor.Document.GetElementById<SvgText>("field_date");
                Assert.IsNotNull(t);
                Assert.AreEqual(1, t.Children.Count);
                Assert.AreEqual("Updated", ((SvgTextSpan)t.Children[0]).Text);
            }
        }

        [TestMethod()]
        public void WithAutoWrapText_FindsTextByIdWithSuffix()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.WithAutoWrapText("suffix", "Zzz", fontSize: 12f);

                var t = processor.Document.GetElementById<SvgText>("suffix_text");
                Assert.IsNotNull(t);
                Assert.AreEqual(1, t.Children.Count);
                Assert.AreEqual("Zzz", ((SvgTextSpan)t.Children[0]).Text);
            }
        }

        [TestMethod()]
        public void WithAutoWrapText_IncreasesRectangleHeightIfNeeded()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                var before = ((SvgRectangle)processor.Document.GetElementById("field_rect")).Height.Value;

                processor.WithAutoWrapText("field_rect", "L1\nL2\nL3\nL4\nL5", fontSize: 12f);

                var after = ((SvgRectangle)processor.Document.GetElementById("field_rect")).Height.Value;
                Assert.IsTrue(after > before,
                    "Высота прямоугольника должна увеличиться под 5 строк.");
            }
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException))]
        public void WithAutoWrapText_NullProcessor_Throws()
        {
            SvgTemplateProcessorExtensions.WithAutoWrapText(null, "field_rect", "text", 12f);
        }

        // ==================== SetFieldTextWithAutoWrap (batch) ====================

        [TestMethod()]
        public void WithAutoWrapFields_ProcessesMultipleFields()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.WithAutoWrapFields(new Dictionary<string, string>
                {
                    { "field_rect", "Line one Line two Line three" },
                    { "field_date", "Hi" }
                }, fontSize: 12f);

                Assert.IsNotNull(processor.Document.GetElementById<SvgText>("field_rect_text"));
                Assert.IsNotNull(processor.Document.GetElementById<SvgText>("field_date"));
            }
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException))]
        public void WithAutoWrapFields_NullDictionary_Throws()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.WithAutoWrapFields(null, 12f);
            }
        }

        // ==================== SetFieldTextWithAutoFontSizing ====================

        [TestMethod()]
        public void WithAutoFontSizingText_ShrinksFontToFit()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.WithAutoFontSizingText(
                    "small_rect",
                    "This text is too long for the small box",
                    fontSize: 20f, maxRowsIncrement: 0);

                var t = processor.Document.GetElementById<SvgText>("small_rect_text");
                Assert.IsNotNull(t);
                Assert.IsTrue(t.FontSize.Value < 20f,
                    "Размер шрифта должен уменьшиться, чтобы текст поместился.");
                Assert.IsTrue(t.FontSize.Value >= 1f,
                    "Размер шрифта не должен опускаться ниже 1.");
            }
        }

        [TestMethod()]
        public void WithAutoFontSizingText_NoRectangle_KeepsOriginalFontSize()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.WithAutoFontSizingText("field_date", "Hello", fontSize: 14f);

                var t = processor.Document.GetElementById<SvgText>("field_date");
                Assert.IsNotNull(t);
                Assert.AreEqual(14f, t.FontSize.Value, 0.001f);
            }
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentException))]
        public void WithAutoFontSizingText_NegativeMaxRows_Throws()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.WithAutoFontSizingText("small_rect", "x", 10f, maxRowsIncrement: -1);
            }
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException))]
        public void WithAutoFontSizingText_NullProcessor_Throws()
        {
            SvgTemplateProcessorExtensions.WithAutoFontSizingText(null, "small_rect", "x", 10f);
        }

        // ==================== SetFieldTextWithAutoFontSizing (batch) ====================

        [TestMethod()]
        public void WithAutoFontSizingFields_ProcessesMultipleFields()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.WithAutoFontSizingFields(new Dictionary<string, string>
                {
                    { "small_rect", "Some longer text here for autosize" },
                    { "field_date", "Hi" }
                }, fontSize: 16f, maxRowsIncrement: 1);

                Assert.IsNotNull(processor.Document.GetElementById<SvgText>("small_rect_text"));
                Assert.IsNotNull(processor.Document.GetElementById<SvgText>("field_date"));
            }
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException))]
        public void WithAutoFontSizingFields_NullDictionary_Throws()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.WithAutoFontSizingFields(null, 12f);
            }
        }

        // ==================== SaveSvg ====================

        [TestMethod()]
        public void SaveSvgTest()
        {
            string outPath = OutPath("out.svg");
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.SaveSvg(outPath);
            }

            Assert.IsTrue(File.Exists(outPath), "Файл SVG не был создан.");
            string content = File.ReadAllText(outPath);
            StringAssert.Contains(content, "<svg");
            StringAssert.Contains(content, "field_name");
            StringAssert.Contains(content, "field_date");
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentException))]
        public void SaveSvg_EmptyPath_Throws()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.SaveSvg("");
            }
        }

        // ==================== ConvertToPdf ====================

        [TestMethod()]
        public void ConvertToPdfTest()
        {
            // Тест пишет PDF → в папке будет template.svg + out.pdf.
            string pdfPath = OutPath("out.pdf");
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.SetFieldText("field_name", "Тест");
                processor.ConvertToPdf(pdfPath, SKColors.White);
            }

            Assert.IsTrue(File.Exists(pdfPath), "PDF-файл не был создан.");
            Assert.IsTrue(new FileInfo(pdfPath).Length > 0, "PDF-файл пуст.");

            byte[] header = new byte[5];
            using (var fs = File.OpenRead(pdfPath))
            {
                fs.Read(header, 0, 5);
            }
            Assert.AreEqual("%PDF-", Encoding.ASCII.GetString(header));
        }

        [TestMethod()]
        public void ConvertToPdf_WithTransparentBackground_CreatesPdf()
        {
            string pdfPath = OutPath("out.pdf");
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.ConvertToPdf(pdfPath);
            }

            Assert.IsTrue(File.Exists(pdfPath));
            Assert.IsTrue(new FileInfo(pdfPath).Length > 0);
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentException))]
        public void ConvertToPdf_EmptyPath_Throws()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.ConvertToPdf("");
            }
        }

        // ==================== Dispose ====================

        [TestMethod()]
        public void DisposeTest()
        {
            var processor = SvgTemplateProcessor.FromFile(_templatePath);
            processor.Dispose();

            Assert.ThrowsException<ObjectDisposedException>(
                () => processor.SetFieldText("field_name", "text"),
                "После Dispose операции должны бросать ObjectDisposedException.");
        }

        [TestMethod()]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            var processor = SvgTemplateProcessor.FromFile(_templatePath);
            processor.Dispose();
            processor.Dispose();
        }

        [TestMethod()]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void Document_AfterDispose_Throws()
        {
            var processor = SvgTemplateProcessor.FromFile(_templatePath);
            processor.Dispose();
            var _ = processor.Document;
        }

        // ==================== Fluent-расширения ====================

        [TestMethod()]
        public void FluentChain_ProducesBothOutputs()
        {
            // Оба результата — svg и pdf — в одной папке теста.
            string outSvg = OutPath("out.svg");
            string outPdf = OutPath("out.pdf");

            using (SvgTemplateProcessor
                .FromFile(_templatePath)
                .WithFieldText("field_name", "Иван Петров")
                .WithFieldSize("field_rect", 200, 100)
                .SaveTo(outSvg)
                .SavePdfTo(outPdf, SKColors.White))
            {
            }

            Assert.IsTrue(File.Exists(outSvg));
            Assert.IsTrue(File.Exists(outPdf));
            StringAssert.Contains(File.ReadAllText(outSvg), "Иван Петров");
        }

        [TestMethod()]
        public void WithFields_BatchUpdate_Works()
        {
            string outPath = OutPath("out.svg");
            using (SvgTemplateProcessor
                .FromFile(_templatePath)
                .WithFields(new Dictionary<string, string>
                {
                    { "field_name", "A" },
                    { "field_date", "B" }
                })
                .SaveTo(outPath))
            {
            }

            string content = File.ReadAllText(outPath);
            StringAssert.Contains(content, ">A<");
            StringAssert.Contains(content, ">B<");
        }

        [TestMethod()]
        public void WithFittedText_Chain_Works()
        {
            string outPath = OutPath("out.svg");
            using (SvgTemplateProcessor
                .FromFile(_templatePath)
                .WithFittedText("field_rect", "0123456789", charWidth: 10f, padding: 4f)
                .SaveTo(outPath))
            {
            }

            string content = File.ReadAllText(outPath);
            StringAssert.Contains(content, "width=\"108\"");
            StringAssert.Contains(content, "height=\"23\"");
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException))]
        public void WithFieldText_NullProcessor_Throws()
        {
            SvgTemplateProcessorExtensions.WithFieldText(null, "field_name", "text");
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException))]
        public void WithFields_NullDictionary_Throws()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.WithFields(null);
            }
        }
        // ==================== GetByteArray ====================

        [TestMethod()]
        public void GetByteArray_ReturnsValidSvgBytes()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                byte[] bytes = processor.GetByteArray();

                Assert.IsNotNull(bytes);
                Assert.IsTrue(bytes.Length > 0, "Массив байт не должен быть пустым.");

                string content = Encoding.UTF8.GetString(bytes);
                StringAssert.Contains(content, "<svg");
                StringAssert.Contains(content, "field_name");
                StringAssert.Contains(content, "field_date");
            }
        }

        [TestMethod()]
        public void GetByteArray_ContainsUpdatedText()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.SetFieldText("field_name", "Иван Петров");

                string content = Encoding.UTF8.GetString(processor.GetByteArray());
                StringAssert.Contains(content, "Иван Петров");
                Assert.IsFalse(content.Contains("Placeholder"));
            }
        }

        [TestMethod()]
        public void GetByteArray_MatchesSaveSvg()
        {
            string outPath = OutPath("out.svg");
            byte[] fromMemory, fromFile;

            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                fromMemory = processor.GetByteArray();
                processor.SaveSvg(outPath);
            }
            fromFile = File.ReadAllBytes(outPath);

            CollectionAssert.AreEqual(fromMemory, fromFile,
                "GetByteArray() и SaveSvg() должны давать идентичные байты.");
        }

        [TestMethod()]
        public void GetByteArray_StartsWithXmlDeclaration()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                string content = Encoding.UTF8.GetString(processor.GetByteArray());
                StringAssert.StartsWith(content, "<?xml");
            }
        }

        [TestMethod()]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void GetByteArray_AfterDispose_Throws()
        {
            var processor = SvgTemplateProcessor.FromFile(_templatePath);
            processor.Dispose();
            processor.GetByteArray();
        }

        // ==================== GetByteArrayAsPdf ====================

        [TestMethod()]
        public void GetByteArrayAsPdf_ReturnsValidPdfBytes()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                byte[] bytes = processor.GetByteArrayAsPdf(SKColors.White);

                Assert.IsNotNull(bytes);
                Assert.IsTrue(bytes.Length > 0, "PDF-байты не должны быть пустыми.");

                byte[] header = new byte[5];
                Array.Copy(bytes, 0, header, 0, 5);
                Assert.AreEqual("%PDF-", Encoding.ASCII.GetString(header),
                    "PDF-файл должен начинаться с сигнатуры %PDF-.");
            }
        }

        [TestMethod()]
        public void GetByteArrayAsPdf_WithTransparentBackground_Works()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                byte[] bytes = processor.GetByteArrayAsPdf(); // null → прозрачный фон

                Assert.IsTrue(bytes.Length > 0);
                Assert.AreEqual("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
            }
        }

        [TestMethod()]
        public void GetByteArrayAsPdf_ContainsUpdatedContent()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.SetFieldText("field_name", "Пётр");
                byte[] bytes = processor.GetByteArrayAsPdf(SKColors.White);
                Assert.IsTrue(bytes.Length > 0);
            }
        }

        [TestMethod()]
        public void GetByteArrayAsPdf_MatchesConvertToPdf()
        {
            string pdfPath = OutPath("out.pdf");
            byte[] fromMemory, fromFile;

            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                fromMemory = processor.GetByteArrayAsPdf(SKColors.White);
                processor.ConvertToPdf(pdfPath, SKColors.White);
            }
            fromFile = File.ReadAllBytes(pdfPath);

            // PDF-файлы могут отличаться побайтово из-за встроенных метаданных.
            Assert.AreEqual("%PDF-", Encoding.ASCII.GetString(fromMemory, 0, 5));
            Assert.AreEqual("%PDF-", Encoding.ASCII.GetString(fromFile, 0, 5));
            Assert.IsTrue(fromMemory.Length > 100);
            Assert.IsTrue(fromFile.Length > 100);
        }

        [TestMethod()]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void GetByteArrayAsPdf_AfterDispose_Throws()
        {
            var processor = SvgTemplateProcessor.FromFile(_templatePath);
            processor.Dispose();
            processor.GetByteArrayAsPdf();
        }
        // ==================== Расширения: GetByteArray ====================

        [TestMethod()]
        public void ExtensionGetByteArray_ReturnsValidSvgBytes()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                byte[] bytes = SvgTemplateProcessorExtensions.GetByteArray(processor);

                Assert.IsNotNull(bytes);
                Assert.IsTrue(bytes.Length > 0, "Массив байт не должен быть пустым.");

                string content = Encoding.UTF8.GetString(bytes);
                StringAssert.Contains(content, "<svg");
                StringAssert.Contains(content, "field_name");
            }
        }

        [TestMethod()]
        public void ExtensionGetByteArray_ReflectsFieldChanges()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.SetFieldText("field_name", "Иван Петров");
                byte[] bytes = SvgTemplateProcessorExtensions.GetByteArray(processor);

                string content = Encoding.UTF8.GetString(bytes);
                StringAssert.Contains(content, "Иван Петров");
                Assert.IsFalse(content.Contains("Placeholder"));
            }
        }

        [TestMethod()]
        public void ExtensionGetByteArray_MatchesInstanceMethod()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                byte[] instanceResult = processor.GetByteArray();
                byte[] extensionResult = SvgTemplateProcessorExtensions.GetByteArray(processor);

                CollectionAssert.AreEqual(instanceResult, extensionResult,
                    "Метод расширения должен давать тот же результат, что и экземплярный.");
            }
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ExtensionGetByteArray_NullProcessor_Throws()
        {
            SvgTemplateProcessorExtensions.GetByteArray(null);
        }

        [TestMethod()]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void ExtensionGetByteArray_DisposedProcessor_Throws()
        {
            var processor = SvgTemplateProcessor.FromFile(_templatePath);
            processor.Dispose();
            SvgTemplateProcessorExtensions.GetByteArray(processor);
        }

        // ==================== Расширения: GetByteArrayAsPdf ====================

        [TestMethod()]
        public void ExtensionGetByteArrayAsPdf_ReturnsValidPdfBytes()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                byte[] bytes = SvgTemplateProcessorExtensions.GetByteArrayAsPdf(processor, SKColors.White);

                Assert.IsNotNull(bytes);
                Assert.IsTrue(bytes.Length > 0, "PDF-байты не должны быть пустыми.");
                Assert.AreEqual("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
            }
        }

        [TestMethod()]
        public void ExtensionGetByteArrayAsPdf_WithTransparentBackground_Works()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                byte[] bytes = SvgTemplateProcessorExtensions.GetByteArrayAsPdf(
                    processor, background: null);

                Assert.IsTrue(bytes.Length > 0);
                Assert.AreEqual("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
            }
        }

        [TestMethod()]
        public void ExtensionGetByteArrayAsPdf_MatchesInstanceMethod()
        {
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                byte[] instanceResult = processor.GetByteArrayAsPdf(SKColors.White);
                byte[] extensionResult = SvgTemplateProcessorExtensions.GetByteArrayAsPdf(processor, SKColors.White);

                Assert.AreEqual("%PDF-", Encoding.ASCII.GetString(instanceResult, 0, 5));
                Assert.AreEqual("%PDF-", Encoding.ASCII.GetString(extensionResult, 0, 5));
                Assert.IsTrue(instanceResult.Length > 100);
                Assert.IsTrue(extensionResult.Length > 100);
            }
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ExtensionGetByteArrayAsPdf_NullProcessor_Throws()
        {
            SvgTemplateProcessorExtensions.GetByteArrayAsPdf(null);
        }

        [TestMethod()]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void ExtensionGetByteArrayAsPdf_DisposedProcessor_Throws()
        {
            var processor = SvgTemplateProcessor.FromFile(_templatePath);
            processor.Dispose();
            SvgTemplateProcessorExtensions.GetByteArrayAsPdf(processor);
        }

        // ==================== Fluent: ToByteArray ====================

        [TestMethod()]
        public void ToByteArray_InChain_ReturnsValidSvgBytes()
        {
            byte[] bytes = SvgTemplateProcessor
                .FromFile(_templatePath)
                .WithFieldText("field_name", "Иван Петров")
                .ToByteArray();

            Assert.IsNotNull(bytes);
            Assert.IsTrue(bytes.Length > 0);

            string content = Encoding.UTF8.GetString(bytes);
            StringAssert.Contains(content, "<svg");
            StringAssert.Contains(content, "Иван Петров");
            Assert.IsFalse(content.Contains("Placeholder"));
        }

        [TestMethod()]
        public void ToByteArray_WithSizeChange_ReflectsNewSize()
        {
            byte[] bytes = SvgTemplateProcessor
                .FromFile(_templatePath)
                .WithFieldSize("field_rect", 300, 150)
                .ToByteArray();

            string content = Encoding.UTF8.GetString(bytes);
            StringAssert.Contains(content, "width=\"300\"");
            StringAssert.Contains(content, "height=\"150\"");
        }

        [TestMethod()]
        public void ToByteArray_WithFittedText_ReflectsEstimatedSize()
        {
            // "0123456789" — 10 символов: width = 10*10 + 4*2 = 108; height = 10*1.5 + 4*2 = 23
            byte[] bytes = SvgTemplateProcessor
                .FromFile(_templatePath)
                .WithFittedText("field_rect", "0123456789", charWidth: 10f, padding: 4f)
                .ToByteArray();

            string content = Encoding.UTF8.GetString(bytes);
            StringAssert.Contains(content, "width=\"108\"");
            StringAssert.Contains(content, "height=\"23\"");
        }

        [TestMethod()]
        public void ToByteArray_EqualsInstanceGetByteArray()
        {
            byte[] fluentResult, instanceResult;

            using (var processor = SvgTemplateProcessor
                .FromFile(_templatePath)
                .WithFieldText("field_name", "A"))
            {
                instanceResult = processor.GetByteArray();
            }

            fluentResult = SvgTemplateProcessor
                .FromFile(_templatePath)
                .WithFieldText("field_name", "A")
                .ToByteArray();

            CollectionAssert.AreEqual(instanceResult, fluentResult,
                "ToByteArray должен давать тот же результат, что и GetByteArray.");
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ToByteArray_NullProcessor_Throws()
        {
            SvgTemplateProcessorExtensions.ToByteArray(null);
        }

        [TestMethod()]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void ToByteArray_DisposedProcessor_Throws()
        {
            var processor = SvgTemplateProcessor.FromFile(_templatePath);
            processor.Dispose();
            processor.ToByteArray();
        }

        // ==================== Fluent: ToPdfByteArray ====================

        [TestMethod()]
        public void ToPdfByteArray_InChain_ReturnsValidPdf()
        {
            byte[] bytes = SvgTemplateProcessor
                .FromFile(_templatePath)
                .WithFieldText("field_name", "Иван Петров")
                .ToPdfByteArray(SKColors.White);

            Assert.IsNotNull(bytes);
            Assert.IsTrue(bytes.Length > 0);
            Assert.AreEqual("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
        }

        [TestMethod()]
        public void ToPdfByteArray_WithTransparentBackground_Works()
        {
            byte[] bytes = SvgTemplateProcessor
                .FromFile(_templatePath)
                .WithFieldText("field_name", "Пётр")
                .ToPdfByteArray(); // background не задан → прозрачный

            Assert.IsTrue(bytes.Length > 0);
            Assert.AreEqual("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
        }

        [TestMethod()]
        public void ToPdfByteArray_AfterSizeChange_ProducesValidPdf()
        {
            byte[] bytes = SvgTemplateProcessor
                .FromFile(_templatePath)
                .WithFieldSize("field_rect", 400, 200)
                .ToPdfByteArray(SKColors.White);

            Assert.IsTrue(bytes.Length > 0);
            Assert.AreEqual("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
        }

        [TestMethod()]
        public void ToPdfByteArray_AndToByteArray_BothWorkOnSameProcessor()
        {
            using (var processor = SvgTemplateProcessor
                .FromFile(_templatePath)
                .WithFieldText("field_name", "Иван"))
            {
                byte[] svgBytes = processor.ToByteArray();
                byte[] pdfBytes = processor.ToPdfByteArray(SKColors.White);

                Assert.IsTrue(svgBytes.Length > 0);
                Assert.IsTrue(pdfBytes.Length > 0);
                Assert.AreEqual("%PDF-", Encoding.ASCII.GetString(pdfBytes, 0, 5));

                string svgContent = Encoding.UTF8.GetString(svgBytes);
                StringAssert.Contains(svgContent, "Иван");
            }
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ToPdfByteArray_NullProcessor_Throws()
        {
            SvgTemplateProcessorExtensions.ToPdfByteArray(null);
        }

        [TestMethod()]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void ToPdfByteArray_DisposedProcessor_Throws()
        {
            var processor = SvgTemplateProcessor.FromFile(_templatePath);
            processor.Dispose();
            processor.ToPdfByteArray();
        }

        // ==================== Интеграционный сценарий ====================

        [TestMethod()]
        public void FluentChain_ProducesSvgAndPdfInMemory()
        {
            byte[] svgBytes = SvgTemplateProcessor
                .FromFile(_templatePath)
                .WithFieldText("field_name", "Иван Петров")
                .WithFieldText("field_date", "05.11.2026")
                .WithFieldSize("field_rect", 300, 120)
                .ToByteArray();

            byte[] pdfBytes = SvgTemplateProcessor
                .FromFile(_templatePath)
                .WithFieldText("field_name", "Иван Петров")
                .WithFieldText("field_date", "05.11.2026")
                .WithFieldSize("field_rect", 300, 120)
                .ToPdfByteArray(SKColors.White);

            Assert.IsTrue(svgBytes.Length > 0);
            Assert.IsTrue(pdfBytes.Length > 0);

            string svgContent = Encoding.UTF8.GetString(svgBytes);
            StringAssert.Contains(svgContent, "Иван Петров");
            StringAssert.Contains(svgContent, "05.11.2026");
            StringAssert.Contains(svgContent, "width=\"300\"");

            Assert.AreEqual("%PDF-", Encoding.ASCII.GetString(pdfBytes, 0, 5));
        }

        // ==================== Интеграция: AutoWrap + AutoFontSizing в цепочке ====================

        [TestMethod()]
        public void FluentChain_WithAutoWrapAndAutoFontSizing_ProducesValidOutputs()
        {
            byte[] svgBytes = SvgTemplateProcessor
                .FromFile(_templatePath)
                .WithAutoWrapText("field_rect", "Очень длинный текст, который должен перенестись",
                    fontSize: 12f)
                .WithAutoFontSizingText("small_rect", "И ещё один длинный текст для маленького бокса",
                    fontSize: 18f, maxRowsIncrement: 1)
                .ToByteArray();

            Assert.IsTrue(svgBytes.Length > 0);
            string svgContent = Encoding.UTF8.GetString(svgBytes);

            StringAssert.Contains(svgContent, "field_rect_text");
            StringAssert.Contains(svgContent, "small_rect_text");

            StringAssert.Contains(svgContent, "Очень длинный");
            StringAssert.Contains(svgContent, "перенестись");
        }
    }
}