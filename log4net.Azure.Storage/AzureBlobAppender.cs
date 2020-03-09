using log4net.Appender;
using log4net.Azure.Storage.Extensions;
using log4net.Core;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.RetryPolicies;

namespace log4net.Azure.Storage
{
    public class AzureBlobAppender : BufferingAppenderSkeleton
    {
        private CloudStorageAccount account;
        private CloudBlobClient client;
        private CloudBlobContainer container;

        public string ConnectionStringName { get; set; }
        public string ConnectionString { get; set; }
        public string ContainerName { get; set; }
        public string DirectoryName { get; set; }
        public string FileName { get; set; }

        public override void ActivateOptions()
        {
            base.ActivateOptions();

            var connectionString = ConnectionString ?? GetConnectionString();

            if (!CloudStorageAccount.TryParse(connectionString, out account))
            {
                throw new ArgumentException("Missing or malformed connection string.", nameof(connectionString));
            }

            client = account.CreateCloudBlobClient();
            container = client.GetContainerReference(ContainerName.ToLower());
            container.CreateIfNotExists();
        }

        protected override void SendBuffer(LoggingEvent[] events)
        {
            Parallel.ForEach(events, ProcessEvent);
        }

        private void ProcessEvent(LoggingEvent loggingEvent)
        {
            try
            {
                var blob = container.GetAppendBlobReference(Path.Combine(DirectoryName, FileName));
                try
                {
                    blob.CreateOrReplace(
                        AccessCondition.GenerateIfNotExistsCondition(),
                        new BlobRequestOptions() { RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(1), 10) },
                        null);
                }
                catch (StorageException ex) when (ex.RequestInformation?.HttpStatusCode == (int)HttpStatusCode.Conflict)
                { }

                var message = loggingEvent.GetFormattedString(Layout);
                blob.AppendText(message, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        private string GetConnectionString()
        {
            try
            {
                var basePath = Directory.GetCurrentDirectory();
                var builder = new ConfigurationBuilder()
                    .SetBasePath(basePath)
                    .AddJsonFile("appsettings.json")
                    .AddEnvironmentVariables();

                var config = builder.Build();
                return config.GetConnectionString(ConnectionStringName);
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}