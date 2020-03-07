using log4net.Core;
using log4net.Layout;
using System;
using System.IO;

namespace log4net.Azure.Storage.Extensions
{
    public static class LoggingEventExtensions
    {
        public static string GetFormattedString(this LoggingEvent loggingEvent, ILayout layout)
        {
            if (layout == null)
            {
                return $"{loggingEvent.RenderedMessage} {loggingEvent.GetExceptionString()}{Environment.NewLine}";
            }

            using var stringWriter = new StringWriter();
            layout.Format(stringWriter, loggingEvent);
            var message = stringWriter.ToString();

            return message;
        }
    }
}
