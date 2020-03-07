using log4net.Appender;
using log4net.Azure.Storage.Extensions;
using log4net.Core;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace log4net.Azure.Storage
{
    public class AzureBlobAppender : BufferingAppenderSkeleton
    {
        private readonly IConfiguration _configuration;

        private CloudStorageAccount _account;
        private CloudBlobClient _client;
        private CloudBlobContainer _container;

        public AzureBlobAppender(IConfiguration configuration)
        {
            this._configuration = configuration;
        }

        public string ConnectionString { get; set; }
        public string ConnectionStringName { get; set; }
        public string ContainerName { get; set; }
        public string DirectoryName { get; set; }
        public string FileName { get; set; }

        public override void ActivateOptions()
        {
            base.ActivateOptions();

            var connectionString = GetConnectionString();

            if (!CloudStorageAccount.TryParse(connectionString, out _account))
            {
                throw new ArgumentException();
            }

            _client = _account.CreateCloudBlobClient();
            _container = _client.GetContainerReference(ContainerName.ToLower());
            _container.CreateIfNotExists();
        }

        protected override void SendBuffer(LoggingEvent[] events)
        {
            Parallel.ForEach(events, loggingEvent => ProcessEvent(loggingEvent));
        }

        private void ProcessEvent(LoggingEvent loggingEvent)
        {
            try
            {
                var blob = _container.GetAppendBlobReference(Path.Combine(DirectoryName, FileName));
                var message = loggingEvent.GetFormattedString(Layout);
                blob.UploadText(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        private string GetConnectionString()
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                if (!string.IsNullOrWhiteSpace(ConnectionStringName))
                {
                    ConnectionString = _configuration?.GetConnectionString(ConnectionStringName);
                }
            }

            return ConnectionString;
        }
    }
}
