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
    }
}
