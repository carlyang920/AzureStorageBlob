using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Azure.Core.Pipeline;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Sample.AzureStorageBlob.Blob.Enums;
using Sample.AzureStorageBlob.Blob.Interfaces;
using Sample.AzureStorageBlob.Blob.Models;
using BlobInfo = Sample.AzureStorageBlob.Blob.Models.BlobInfo;

namespace Sample.AzureStorageBlob.Blob.Services
{
    public class BlobService : IBlobService
    {
        private readonly BlobContainerClient _blobContainerClient;

        private int _recursiveCount;

        public BlobService(BlobConfigInfo blobInfo)
        {
            _blobContainerClient = GetContainerClient(
                blobInfo.AzureStorageConnectionString,
                blobInfo.ContainerName.ToLower(),
                blobInfo.ProxyUrl
                );
        }

        #region private function

        private async Task<MemoryStream> GetMemoryStream(
            string content,
            Encoding encoding = null,
            bool isWriteBomBytes = false
            )
        {
            if (null == encoding)
                encoding = new UTF8Encoding();

            var contentsBytes = encoding.GetBytes(content);

            var stream = new MemoryStream();

            if (isWriteBomBytes)
            {
                var preamble = encoding.GetPreamble();

                //Write BOM bytes
                await stream.WriteAsync(preamble, 0, preamble.Length);
            }

            await stream.WriteAsync(contentsBytes, 0, contentsBytes.Length);
            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        /// <summary>
        /// Separate parameters of connection.
        /// </summary>
        /// <param name="azureStorageConnectionString"></param>
        private Dictionary<string, string> GetConnectionInfo(string azureStorageConnectionString)
        {
            return string.IsNullOrEmpty(azureStorageConnectionString) ?
                new Dictionary<string, string>()
                :
                azureStorageConnectionString.Split(';').ToDictionary(
                    element => element.Substring(0, element.IndexOf("=", StringComparison.Ordinal)),
                    element => element.Substring(element.IndexOf("=", StringComparison.Ordinal) + 1)
                );
        }

        /// <summary>
        /// Get specific blob container client
        /// </summary>
        /// <param name="storageConnectionString"></param>
        /// <param name="blobContainerName"></param>
        /// <param name="proxyUrl"></param>
        private BlobContainerClient GetContainerClient(
            string storageConnectionString,
            string blobContainerName,
            string proxyUrl = null
            )
        {
            proxyUrl = proxyUrl?.Trim();

            if (string.IsNullOrEmpty(storageConnectionString) || string.IsNullOrEmpty(blobContainerName.ToLower()))
                throw new ArgumentNullException($"storageConnectionString: {storageConnectionString}, blobContainerName: {blobContainerName}");

            var currentServiceClient = new BlobServiceClient(storageConnectionString);

            var dict = GetConnectionInfo(storageConnectionString);
            var credential = new StorageSharedKeyCredential(dict["AccountName"], dict["AccountKey"]);

            var currentBlobContainerClient = string.IsNullOrEmpty(proxyUrl) ?
                new BlobContainerClient(new Uri($"{currentServiceClient.Uri.AbsoluteUri}{blobContainerName.ToLower()}"), credential)
                :
                new BlobContainerClient(new Uri($"{currentServiceClient.Uri.AbsoluteUri}{blobContainerName.ToLower()}"), credential, new BlobClientOptions()
                {
                    Transport = new HttpClientTransport(new HttpClientHandler()
                    {
                        Proxy = new WebProxy(new Uri(proxyUrl), true)
                    })
                });

            if (!currentBlobContainerClient.Exists()) currentBlobContainerClient.Create();

            return currentBlobContainerClient;
        }

        /// <summary>
        /// Recursively seeking 10 level of inner exception.
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        private string GetInnerExpMsg(
            Exception ex
            )
        {
            _recursiveCount += 1;

            //最多遞迴10次
            if (10 <= _recursiveCount)
                return string.Empty;

            var sb = new StringBuilder();

            sb.AppendLine(null != ex.InnerException ? GetInnerExpMsg(ex.InnerException) : ex.ToString());

            return sb.ToString();
        }

        private BlobContainerSasPermissions GetPermissions(
            BlobContainerPermissions type
        )
        {
            switch (type)
            {
                case BlobContainerPermissions.Read:
                    return (BlobContainerSasPermissions.Read | BlobContainerSasPermissions.List);
                case BlobContainerPermissions.Write:
                    return (BlobContainerSasPermissions.Read | BlobContainerSasPermissions.List | BlobContainerSasPermissions.Add | BlobContainerSasPermissions.Create | BlobContainerSasPermissions.Write);
                case BlobContainerPermissions.Delete:
                    return (BlobContainerSasPermissions.Read | BlobContainerSasPermissions.List | BlobContainerSasPermissions.Add | BlobContainerSasPermissions.Create | BlobContainerSasPermissions.Write | BlobContainerSasPermissions.Delete);
                case BlobContainerPermissions.All:
                    return BlobContainerSasPermissions.All;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private string GetNextFileName(List<string> zipFiles, string fileName)
        {
            var extension = Path.GetExtension(fileName);
            var fileNameOnly = $"/{Directory.GetParent(fileName).Name}/{Path.GetFileName(fileName)}";
            var i = 0;

            // If the file exists, keep trying until it doesn't
            while (zipFiles.Contains(fileName))
            {
                i += 1;
                fileName = string.Format("{0}({1}){2}", fileNameOnly, i, extension);
            }
            return fileName;
        }

        #endregion

        #region public function

        /// <summary>
        /// Check current container exists or not
        /// </summary>
        /// <returns></returns>
        public bool CheckBlobContainer()
        {
            return _blobContainerClient.Exists();
        }

        /// <summary>
        /// Check blob file exists or not
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task<bool> CheckBlobFileExistsAsync(string fileName)
        {
            try
            {
                var blobClient = _blobContainerClient.GetBlobClient(fileName);

                return await blobClient.ExistsAsync();
            }
            catch (Exception e)
            {
                var innerExpMsg = GetInnerExpMsg(e);
                _recursiveCount = 0;

                if (!string.IsNullOrEmpty(innerExpMsg))
                    e.Data.Add($"{MethodBase.GetCurrentMethod().DeclaringType?.Name}_{Guid.NewGuid().ToString().Replace("-", "")}", innerExpMsg);

                throw;
            }
        }

        /// <summary>
        /// To query files by path hierarchy
        /// </summary>
        /// <param name="prefix">The searching prefix key word.</param>
        /// <returns></returns>
        public async Task<IEnumerable<BlobInfo>> QueryFilesAsync(string prefix)
        {
            var blobs = new List<BlobInfo>();

            try
            {
                // Call the listing operation and return pages of the specified size.
                var results = _blobContainerClient.GetBlobsByHierarchyAsync(prefix: prefix, delimiter: "/")
                    .AsPages(default, 500);

                // Enumerate the blobs returned for each page.
                await foreach (var blobPage in results)
                {
                    // A hierarchical listing may return both virtual directories and blobs.
                    foreach (var blobHierarchyItem in blobPage.Values)
                    {
                        if (!blobHierarchyItem.IsBlob) continue;

                        var blobItem = blobHierarchyItem.Blob;

                        blobs.Add(new BlobInfo
                        {
                            Uri = $"{_blobContainerClient.Uri.AbsoluteUri}/{blobItem.Name}",
                            BlobType = Enum.GetName(typeof(BlobType), blobItem.Properties.BlobType ?? BlobType.Block),
                            FileName = Path.GetFileName(blobItem.Name),
                            BlobPath = Path.GetDirectoryName(blobItem.Name)?.Replace("\\", "/"),
                            CreatedTime = blobItem.Properties.CreatedOn?.DateTime,
                            LastModifiedTime = blobItem.Properties.LastModified?.DateTime
                        });
                    }
                }
            }
            catch (Exception e)
            {
                var innerExpMsg = GetInnerExpMsg(e);
                _recursiveCount = 0;

                if (!string.IsNullOrEmpty(innerExpMsg))
                    e.Data.Add(
                        $"{MethodBase.GetCurrentMethod().DeclaringType?.Name}_{Guid.NewGuid().ToString().Replace("-", "")}",
                        innerExpMsg);

                throw;
            }

            return blobs;
        }

        /// <summary>
        /// To upload block blob file with data
        /// </summary>
        /// <param name="fileName">The Azure blob file path.</param>
        /// <param name="content">File content.</param>
        /// <param name="encoding"></param>
        /// <param name="isWriteBomBytes"></param>
        /// <returns></returns>
        public async Task CreateFileAsync(
            string fileName,
            string content,
            Encoding encoding = null,
            bool isWriteBomBytes = false
            )
        {
            try
            {
                using var memStream = await GetMemoryStream(content, encoding, isWriteBomBytes);

                //Delete file first
                await _blobContainerClient.DeleteBlobIfExistsAsync(fileName);

                await _blobContainerClient.UploadBlobAsync(
                    fileName,
                    memStream
                );
            }
            catch (Exception e)
            {
                var innerExpMsg = GetInnerExpMsg(e);
                _recursiveCount = 0;

                if (!string.IsNullOrEmpty(innerExpMsg))
                    e.Data.Add($"{MethodBase.GetCurrentMethod().DeclaringType?.Name}_{Guid.NewGuid().ToString().Replace("-", "")}", innerExpMsg);

                throw;
            }
        }

        /// <summary>
        /// To append data into specific file
        /// </summary>
        /// <param name="relativeFilePath"></param>
        /// <param name="content"></param>
        /// <param name="encoding"></param>
        /// <param name="isWriteBomBytes"></param>
        /// <returns></returns>
        public async Task AppendFileAsync(
            string relativeFilePath,
            string content,
            Encoding encoding = null,
            bool isWriteBomBytes = false
            )
        {
            try
            {
                using var memStream = await GetMemoryStream(content, encoding, isWriteBomBytes);

                var appendBlob = _blobContainerClient.GetAppendBlobClient(relativeFilePath);

                if (!appendBlob.Exists()) appendBlob.Create();

                await appendBlob.AppendBlockAsync(memStream);
            }
            catch (Exception e)
            {
                var innerExpMsg = GetInnerExpMsg(e);
                _recursiveCount = 0;

                if (!string.IsNullOrEmpty(innerExpMsg))
                    e.Data.Add($"{MethodBase.GetCurrentMethod().DeclaringType?.Name}_{Guid.NewGuid().ToString().Replace("-", "")}", innerExpMsg);

                throw;
            }
        }

        /// <summary>
        /// To download file content as string
        /// </summary>
        /// <param name="fileName">The Azure blob file path.</param>
        /// <returns></returns>
        public async Task<string> DownloadFileAsStringAsync(
            string fileName
            )
        {
            try
            {
                var content = string.Empty;
                var blobClient = _blobContainerClient.GetBlobClient(fileName);

                if (!blobClient.Exists()) return content;

                var downloadBlobInfo = await blobClient.DownloadAsync();

                using (var reader = new StreamReader(downloadBlobInfo.Value.Content))
                {
                    content = await reader.ReadToEndAsync();
                }

                return content;
            }
            catch (Exception e)
            {
                var innerExpMsg = GetInnerExpMsg(e);
                _recursiveCount = 0;

                if (!string.IsNullOrEmpty(innerExpMsg))
                    e.Data.Add($"{MethodBase.GetCurrentMethod().DeclaringType?.Name}_{Guid.NewGuid().ToString().Replace("-", "")}", innerExpMsg);

                throw;
            }
        }

        /// <summary>
        /// To delete single file
        /// </summary>
        /// <param name="fileName">The Azure blob file path.</param>
        /// <returns></returns>
        public async Task DeleteFileAsync(
            string fileName
            )
        {
            try
            {
                var blobClient = _blobContainerClient.GetBlobClient(fileName);

                await blobClient.DeleteAsync();
            }
            catch (Exception e)
            {
                var innerExpMsg = GetInnerExpMsg(e);
                _recursiveCount = 0;

                if (!string.IsNullOrEmpty(innerExpMsg))
                    e.Data.Add($"{MethodBase.GetCurrentMethod().DeclaringType?.Name}_{Guid.NewGuid().ToString().Replace("-", "")}", innerExpMsg);

                throw;
            }
        }

        /// <summary>
        /// Delete specific container if exists
        /// </summary>
        /// <returns></returns>
        public async Task DeleteContainerIfExistsAsync()
        {
            try
            {
                await _blobContainerClient.DeleteIfExistsAsync();
            }
            catch (Exception e)
            {
                var innerExpMsg = GetInnerExpMsg(e);
                _recursiveCount = 0;

                if (!string.IsNullOrEmpty(innerExpMsg))
                    e.Data.Add($"{MethodBase.GetCurrentMethod().DeclaringType?.Name}_{Guid.NewGuid().ToString().Replace("-", "")}", innerExpMsg);

                throw;
            }
        }

        /// <summary>
        /// To upload single file
        /// </summary>
        /// <param name="prefixPath">The Azure blob prefix path.</param>
        /// <param name="localFileFullPath">The local file path.</param>
        /// <param name="blobPath">The Azure blob target path.</param>
        public void UploadFromFile(
            string prefixPath,
            string localFileFullPath,
            string blobPath
            )
        {
            try
            {
                // Get a reference to a blob named "sample-file" in a container named "sample-container"
                var blobFile = _blobContainerClient.GetBlobClient(Path.Combine(prefixPath, blobPath));

                // Upload local file
                blobFile.Upload(localFileFullPath);
            }
            catch (Exception e)
            {
                var innerExpMsg = GetInnerExpMsg(e);
                _recursiveCount = 0;

                if (!string.IsNullOrEmpty(innerExpMsg))
                    e.Data.Add($"{MethodBase.GetCurrentMethod().DeclaringType?.Name}_{Guid.NewGuid().ToString().Replace("-", "")}", innerExpMsg);

                throw;
            }
        }

        /// <summary>
        /// To download file to local path
        /// </summary>
        /// <param name="blobPath"></param>
        /// <param name="saveAsPath">Path to save on local drive</param>
        /// <param name="printInfo">Flag for print downloading information</param>
        public async Task DownloadFileAsync(
            string blobPath,
            string saveAsPath,
            bool printInfo = false
            )
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(saveAsPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(saveAsPath) ?? AppDomain.CurrentDomain.BaseDirectory);
                }

                var blobClient = _blobContainerClient.GetBlobClient(blobPath);

                await blobClient.DownloadToAsync($"{Path.GetDirectoryName(saveAsPath)}\\{Path.GetFileName(saveAsPath)?.Replace("-", "").Replace("_", "").Replace(" ", "").Replace(":", "")}");

                if (printInfo)
                {
                    Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}, {blobPath} has been download.");
                }
            }
            catch (Exception e)
            {
                var innerExpMsg = GetInnerExpMsg(e);
                _recursiveCount = 0;

                if (!string.IsNullOrEmpty(innerExpMsg))
                    e.Data.Add($"{MethodBase.GetCurrentMethod().DeclaringType?.Name}_{Guid.NewGuid().ToString().Replace("-", "")}", innerExpMsg);

                throw;
            }
        }

        /// <summary>
        /// To synchronize file(s) from current container to target container
        /// </summary>
        /// <param name="targetBlobInfo">Destination blob information</param>
        /// <param name="sourceBlobPath">Source file path</param>
        /// <param name="prefixSource">Prefix of source</param>
        /// <param name="prefixTarget">Prefix of destination</param>
        /// <param name="printConsole">Export information to console</param>
        public async Task SyncSourceBlobAsync(
            BlobConfigInfo targetBlobInfo,
            string sourceBlobPath,
            string prefixSource = "",
            string prefixTarget = "",
            bool printConsole = false
            )
        {
            try
            {
                //Get Target Storage Blob Container Info.
                if (string.IsNullOrEmpty(targetBlobInfo.AzureStorageConnectionString)) return;

                var targetBlobContainerClient = GetContainerClient(
                    targetBlobInfo.AzureStorageConnectionString,
                    targetBlobInfo.ContainerName,
                    targetBlobInfo.ProxyUrl
                );

                //Get Source Blob Container Info.
                var sourceSas = GetBlobContainerSasToken(
                    null,
                    BlobContainerPermissions.All,
                    DateTimeOffset.UtcNow.AddHours(24)
                    );

                var sourceUri = $"{_blobContainerClient.Uri}/{sourceBlobPath}{sourceSas}";

                var targetFilePath = sourceBlobPath.Replace(prefixSource, prefixTarget);

                var targetBlobClient = targetBlobContainerClient.GetBlobClient(targetFilePath);

                await targetBlobClient.StartCopyFromUriAsync(new Uri(sourceUri));

                if (printConsole)
                {
                    Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}, {targetFilePath} has been copied.");
                }
            }
            catch (Exception e)
            {
                var innerExpMsg = GetInnerExpMsg(e);

                if (!string.IsNullOrEmpty(innerExpMsg))
                    e.Data.Add($"{MethodBase.GetCurrentMethod().DeclaringType?.Name}_{Guid.NewGuid().ToString().Replace("-", "")}", innerExpMsg);

                throw;
            }
        }

        /// <summary>
        /// To count files in specific path hierarchy
        /// </summary>
        /// <param name="prefix">virtual path</param>
        /// <returns>files count</returns>
        public int GetBlobFilesCount(string prefix)
        {
            try
            {
                return QueryFilesAsync(prefix).Result.ToList().Count;
            }
            catch (Exception e)
            {
                var innerExpMsg = GetInnerExpMsg(e);

                if (!string.IsNullOrEmpty(innerExpMsg))
                    e.Data.Add($"{MethodBase.GetCurrentMethod().DeclaringType?.Name}_{Guid.NewGuid().ToString().Replace("-", "")}", innerExpMsg);

                throw;
            }
        }

        /// <summary>
        /// Get current container's URI
        /// </summary>
        /// <returns></returns>
        public Uri GetContainerUri()
        {
            return _blobContainerClient?.Uri;
        }

        /// <summary>
        /// Get SAS token from current or specific container
        /// </summary>
        /// <param name="targetBlobInfo"></param>
        /// <param name="permission"></param>
        /// <param name="expiryTime"></param>
        /// <param name="isUseCurrentBlobContainer"></param>
        /// <returns></returns>
        public string GetBlobContainerSasToken(
            BlobConfigInfo targetBlobInfo,
            BlobContainerPermissions permission,
            DateTimeOffset expiryTime,
            bool isUseCurrentBlobContainer = true
            )
        {
            BlobContainerClient currentBlobContainerClient;

            if (isUseCurrentBlobContainer)
            {
                currentBlobContainerClient = _blobContainerClient;
            }
            else
            {
                currentBlobContainerClient = GetContainerClient(
                    targetBlobInfo.AzureStorageConnectionString,
                    targetBlobInfo.ContainerName,
                    targetBlobInfo.ProxyUrl
                    );
            }

            // Check whether this BlobContainerClient object has been authorized with Shared Key.
            if (!currentBlobContainerClient.CanGenerateSasUri) return null;

            // Create a SAS token that's valid for one hour.
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = currentBlobContainerClient.Name,
                Resource = "c", //b for Blob, c for Blob Container
                ExpiresOn = expiryTime
            };

            sasBuilder.SetPermissions(GetPermissions(permission));

            var sasToken = currentBlobContainerClient.GenerateSasUri(sasBuilder).Query;

            return sasToken;
        }

        #endregion
    }
}
