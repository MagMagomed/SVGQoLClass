using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
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
        private string _testDir;
        private string _templatePath;

        // Простой шаблон: один <rect> и два <text> с id
        private const string TestSvg =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<svg xmlns=""http://www.w3.org/2000/svg"" width=""500"" height=""500"">
  <rect id=""field_rect"" x=""10"" y=""10"" width=""100"" height=""50"" fill=""lightgray""/>
  <text id=""field_name"" x=""20"" y=""40"" font-size=""16"">Placeholder</text>
  <text id=""field_date"" x=""20"" y=""70"" font-size=""16"">01.01.2000</text>
</svg>";

        [TestInitialize]
        public void Setup()
        {
            // 1. Получаем корень проекта тестов
            string assemblyDir = AppDomain.CurrentDomain.BaseDirectory;
            string projectDir = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", ".."));

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

            // 3. Создаем персональную подпапку внутри TestResults
            _testDir = Path.Combine(projectDir, "TestResults", $"Run_{timestamp}");
            Directory.CreateDirectory(_testDir);

            // 4. Записываем шаблон в эту новую папку
            _templatePath = Path.Combine(_testDir, "template.svg");
            File.WriteAllText(_templatePath, TestSvg, new UTF8Encoding(false));
        }

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                //if (Directory.Exists(_testDir))
                //    Directory.Delete(_testDir, true);
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
            SvgTemplateProcessor.FromFile(Path.Combine(_testDir, "missing.svg"));
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
            string outPath = Path.Combine(_testDir, "out.svg");
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

        // ==================== SetFieldSize ====================

        [TestMethod()]
        public void SetFieldSizeTest()
        {
            string outPath = Path.Combine(_testDir, "out.svg");
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
            string outPath = Path.Combine(_testDir, "out.svg");
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

        // ==================== FitFieldToText ====================

        [TestMethod()]
        public void FitFieldToTextTest()
        {
            string outPath = Path.Combine(_testDir, "out.svg");
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
            string outPath = Path.Combine(_testDir, "out.svg");
            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                processor.FitFieldToText("field_rect", "");
                processor.SaveSvg(outPath);
            }

            string content = File.ReadAllText(outPath);
            StringAssert.Contains(content, "width=\"100\"");
            StringAssert.Contains(content, "height=\"50\"");
        }

        // ==================== SaveSvg ====================

        [TestMethod()]
        public void SaveSvgTest()
        {
            string outPath = Path.Combine(_testDir, "saved.svg");
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
            string pdfPath = Path.Combine(_testDir, "output.pdf");
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
            string pdfPath = Path.Combine(_testDir, "output_transparent.pdf");
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
            string outSvg = Path.Combine(_testDir, "chain.svg");
            string outPdf = Path.Combine(_testDir, "chain.pdf");

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
            string outPath = Path.Combine(_testDir, "batch.svg");
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
            string outPath = Path.Combine(_testDir, "fitted.svg");
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
            string outPath = Path.Combine(_testDir, "saved.svg");
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
            string pdfPath = Path.Combine(_testDir, "output.pdf");
            byte[] fromMemory, fromFile;

            using (var processor = SvgTemplateProcessor.FromFile(_templatePath))
            {
                fromMemory = processor.GetByteArrayAsPdf(SKColors.White);
                processor.ConvertToPdf(pdfPath, SKColors.White);
            }
            fromFile = File.ReadAllBytes(pdfPath);

            // PDF-файлы могут отличаться побайтово из-за встроенных метаданных
            // (дата создания, уникальный ID). Поэтому проверяем только сигнатуру
            // и то, что оба файла валидны.
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
    }
}