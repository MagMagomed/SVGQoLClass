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
        /// Возвращает текущее состояние SVG-документа в виде массива байт.
        /// </summary>
        /// <param name="processor">Целевой процессор.</param>
        /// <returns>Массив байт с сериализованным SVG.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="processor"/> равен <c>null</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Экземпляр уже освобождён.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Является обёрткой над <see cref="SvgTemplateProcessor.GetByteArray"/>.
        /// </para>
        /// <para>
        /// <b>Внимание:</b> сигнатура совпадает с экземплярным методом, поэтому
        /// компилятор всегда предпочтёт экземплярный вызов, и это расширение
        /// фактически не будет использовано в коде. Метод добавлен для симметрии
        /// публичного API. Для fluent-цепочек используйте <see cref="ToByteArray"/>.
        /// </para>
        /// </remarks>
        public static byte[] GetByteArray(this SvgTemplateProcessor processor)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));
            return processor.GetByteArray();
        }

        /// <summary>
        /// Возвращает PDF-представление текущего SVG-документа в виде массива байт.
        /// </summary>
        /// <param name="processor">Целевой процессор.</param>
        /// <param name="background">
        /// Цвет фона PDF. Если <c>null</c>, используется прозрачный фон.
        /// </param>
        /// <returns>Массив байт с PDF-документом.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="processor"/> равен <c>null</c>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Экземпляр уже освобождён.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Не удалось отрендерить SVG в PDF (см. <see cref="SvgTemplateProcessor.GetByteArrayAsPdf"/>).
        /// </exception>
        /// <remarks>
        /// <b>Внимание:</b> как и <see cref="GetByteArray"/>, эта перегрузка
        /// перекрывается экземплярным методом. Для fluent-цепочек используйте
        /// <see cref="ToPdfByteArray"/>.
        /// </remarks>
        public static byte[] GetByteArrayAsPdf(
            this SvgTemplateProcessor processor, SKColor? background = null)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));
            return processor.GetByteArrayAsPdf(background);
        }

        // ==================== FLUENT-FRIENDLY АЛЬТЕРНАТИВЫ ====================

        /// <summary>
        /// Терминальная операция fluent-цепочки: возвращает SVG как массив байт.
        /// В отличие от <see cref="GetByteArray"/>, это имя не перекрывается
        /// экземплярным методом и реально используется в цепочках.
        /// </summary>
        /// <param name="processor">Целевой процессор.</param>
        /// <returns>Массив байт с сериализованным SVG.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="processor"/> равен <c>null</c>.
        /// </exception>
        /// <example>
        /// <code>
        /// byte[] bytes = SvgTemplateProcessor
        ///     .FromFile("template.svg")
        ///     .WithFieldText("field_name", "Иван Петров")
        ///     .ToByteArray();
        /// </code>
        /// </example>
        public static byte[] ToByteArray(this SvgTemplateProcessor processor)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));
            return processor.GetByteArray();
        }

        /// <summary>
        /// Терминальная операция fluent-цепочки: возвращает PDF как массив байт.
        /// </summary>
        /// <param name="processor">Целевой процессор.</param>
        /// <param name="background">
        /// Цвет фона PDF. Если <c>null</c>, используется прозрачный фон.
        /// </param>
        /// <returns>Массив байт с PDF-документом.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="processor"/> равен <c>null</c>.
        /// </exception>
        /// <example>
        /// <code>
        /// byte[] pdf = SvgTemplateProcessor
        ///     .FromFile("template.svg")
        ///     .WithFieldText("field_name", "Иван Петров")
        ///     .ToPdfByteArray(SKColors.White);
        /// </code>
        /// </example>
        public static byte[] ToPdfByteArray(
            this SvgTemplateProcessor processor, SKColor? background = null)
        {
            if (processor == null) throw new ArgumentNullException(nameof(processor));
            return processor.GetByteArrayAsPdf(background);
        }
        /// <summary>
        /// Fluent-обёртка над <see cref="SvgTemplateProcessor.SetFieldTextWithAutoWrap"/>:
        /// устанавливает текст с автоматическим переносом строк, при необходимости
        /// создавая <c>SvgText</c> поверх <c>SvgRectangle</c> с тем же id и увеличивая
        /// высоту прямоугольника под число строк.
        /// </summary>
        /// <param name="processor">Целевой процессор.</param>
        /// <param name="fieldId">
        /// id текстового элемента или прямоугольника, поверх которого нужно
        /// разместить текст.
        /// </param>
        /// <param name="text">Текст для вставки.</param>
        /// <param name="fontSize">Размер шрифта в единицах SVG.</param>
        /// <param name="padding">Внутренний отступ от краёв прямоугольника.</param>
        /// <param name="charWidthFactor">
        /// Приблизительная ширина одного символа относительно размера шрифта.
        /// Используется для эвристического переноса строк.
        /// </param>
        /// <returns>Тот же экземпляр <paramref name="processor"/> для продолжения цепочки.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="processor"/> равен <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="fieldId"/> не задан, либо элемент с таким id не найден,
        /// либо размер шрифта не положителен.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Экземпляр уже освобождён.
        /// </exception>
        /// <example>
        /// <code>
        /// SvgTemplateProcessor
        ///     .FromFile("template.svg")
        ///     .WithAutoWrapText("field_rect", "Очень длинный текст…", fontSize: 14)
        ///     .SaveTo("output.svg");
        /// </code>
        /// </example>
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
        /// Пакетная fluent-обёртка над <see cref="SvgTemplateProcessor.SetFieldTextWithAutoWrap"/>:
        /// последовательно применяет авто-перенос к нескольким полям.
        /// Удобна, когда поля задаются из словаря (например, из БД или JSON).
        /// </summary>
        /// <param name="processor">Целевой процессор.</param>
        /// <param name="fields">Словарь «id поля → текст».</param>
        /// <param name="fontSize">Размер шрифта, применяемый ко всем полям.</param>
        /// <param name="padding">Внутренний отступ от краёв прямоугольников.</param>
        /// <param name="charWidthFactor">
        /// Приблизительная ширина одного символа относительно размера шрифта.
        /// </param>
        /// <returns>Тот же экземпляр <paramref name="processor"/> для продолжения цепочки.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="processor"/> или <paramref name="fields"/> равны <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Один из id не найден, либо размер шрифта не положителен.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Экземпляр уже освобождён.
        /// </exception>
        /// <example>
        /// <code>
        /// var data = new Dictionary&lt;string, string&gt;
        /// {
        ///     ["field_fio"]  = "Иван Петров",
        ///     ["field_addr"] = "г. Москва, ул. Ленина, д. 1",
        /// };
        ///
        /// SvgTemplateProcessor
        ///     .FromFile("template.svg")
        ///     .WithAutoWrapFields(data, fontSize: 12)
        ///     .SavePdfTo("output.pdf", SKColors.White);
        /// </code>
        /// </example>
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
    }
}
