using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SVGQoLClass
{
    public static class SvgTemplateProcessorExtensions
    {
        public static SvgTemplateProcessor WithFieldText(
            this SvgTemplateProcessor processor, string fieldId, string text)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));
            processor.SetFieldText(fieldId, text);
            return processor;
        }

        public static SvgTemplateProcessor WithFieldSize(
            this SvgTemplateProcessor processor, string fieldId, float width, float height)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));
            processor.SetFieldSize(fieldId, width, height);
            return processor;
        }

        public static SvgTemplateProcessor WithFittedText(
            this SvgTemplateProcessor processor, string fieldId, string text,
            float charWidth = 10f, float padding = 4f)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));
            processor.FitFieldToText(fieldId, text, charWidth, padding);
            return processor;
        }

        /// <summary>Пакетная установка нескольких полей за один вызов.</summary>
        public static SvgTemplateProcessor WithFields(
            this SvgTemplateProcessor processor, IDictionary<string, string> fields)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));
            if (fields == null) throw new ArgumentNullException(nameof(fields));
            foreach (var kvp in fields)
                processor.SetFieldText(kvp.Key, kvp.Value);
            return processor;
        }

        public static SvgTemplateProcessor SaveTo(
            this SvgTemplateProcessor processor, string path)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));
            processor.SaveSvg(path);
            return processor;
        }

        public static SvgTemplateProcessor SavePdfTo(
            this SvgTemplateProcessor processor, string path, SKColor background)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));
            processor.ConvertToPdf(path, background);
            return processor;
        }

        /// <summary>
        /// Обёртка над <see cref="SvgTemplateProcessor.GetByteArray"/>.
        /// Перекрывается экземплярным методом; для цепочек используйте <see cref="ToByteArray"/>.
        /// </summary>
        public static byte[] GetByteArray(this SvgTemplateProcessor processor)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));
            return processor.GetByteArray();
        }

        /// <summary>
        /// Обёртка над <see cref="SvgTemplateProcessor.GetByteArrayAsPdf"/>.
        /// Перекрывается экземплярным методом; для цепочек используйте <see cref="ToPdfByteArray"/>.
        /// </summary>
        public static byte[] GetByteArrayAsPdf(
            this SvgTemplateProcessor processor, SKColor? background = null)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));
            return processor.GetByteArrayAsPdf(background);
        }

        #region FLUENT-FRIENDLY АЛЬТЕРНАТИВЫ

        /// <summary>
        /// Терминальная операция fluent-цепочки: возвращает SVG как массив байт.
        /// </summary>
        public static byte[] ToByteArray(this SvgTemplateProcessor processor)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));
            return processor.GetByteArray();
        }

        /// <summary>
        /// Терминальная операция fluent-цепочки: возвращает PDF как массив байт.
        /// </summary>
        public static byte[] ToPdfByteArray(
            this SvgTemplateProcessor processor, SKColor? background = null)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));
            return processor.GetByteArrayAsPdf(background);
        }

        /// <summary>
        /// Fluent-обёртка над <see cref="SvgTemplateProcessor.SetFieldTextWithAutoWrap"/>.
        /// </summary>
        public static SvgTemplateProcessor WithAutoWrapText(
            this SvgTemplateProcessor processor,
            string fieldId,
            string text,
            float fontSize,
            float padding = 4f,
            float charWidthFactor = 0.55f,
            SKColor? fontColor = null)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));
            processor.SetFieldTextWithAutoWrap(fieldId, text, fontSize, padding, charWidthFactor, fontColor);
            return processor;
        }

        /// <summary>
        /// Пакетная fluent-обёртка над <see cref="SvgTemplateProcessor.SetFieldTextWithAutoWrap"/>.
        /// </summary>
        public static SvgTemplateProcessor WithAutoWrapFields(
            this SvgTemplateProcessor processor,
            IDictionary<string, string> fields,
            float fontSize,
            float padding = 4f,
            float charWidthFactor = 0.55f,
            SKColor? fontColor = null)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));
            if (fields == null) throw new ArgumentNullException(nameof(fields));

            foreach (var kvp in fields)
                processor.SetFieldTextWithAutoWrap(kvp.Key, kvp.Value, fontSize, padding, charWidthFactor, fontColor);

            return processor;
        }

        /// <summary>
        /// Fluent-обёртка над <see cref="SvgTemplateProcessor.SetFieldTextWithAutoFontSizing"/>.
        /// </summary>
        public static SvgTemplateProcessor WithAutoFontSizingText(
            this SvgTemplateProcessor processor,
            string fieldId,
            string text,
            float fontSize,
            int maxRowsIncrement = 3,
            float padding = 4f,
            float charWidthFactor = 0.55f,
            SKColor? fontColor = null)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));
            processor.SetFieldTextWithAutoFontSizing(
                fieldId, text, fontSize,
                maxRowsIncrement, padding, charWidthFactor, fontColor);
            return processor;
        }

        /// <summary>
        /// Пакетная fluent-обёртка над <see cref="SvgTemplateProcessor.SetFieldTextWithAutoFontSizing"/>.
        /// </summary>
        public static SvgTemplateProcessor WithAutoFontSizingFields(
            this SvgTemplateProcessor processor,
            IDictionary<string, string> fields,
            float fontSize,
            int maxRowsIncrement = 3,
            float padding = 4f,
            float charWidthFactor = 0.55f,
            SKColor? fontColor = null)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));
            if (fields == null) throw new ArgumentNullException(nameof(fields));

            foreach (var kvp in fields)
                processor.SetFieldTextWithAutoFontSizing(
                    kvp.Key, kvp.Value, fontSize,
                    maxRowsIncrement, padding, charWidthFactor, fontColor);

            return processor;
        }
        #endregion
    }
}