using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sample.AzureStorageBlob.Blob.Enums;
using Sample.AzureStorageBlob.Blob.Interfaces;
using Sample.AzureStorageBlob.Blob.Models;
using Sample.AzureStorageBlob.Blob.Services;

namespace Sample.AzureStorageBlob.Test.Blob
{
    [TestClass]
    public class BlobServiceTest
    {
        private readonly IBlobService _service;

        private string _azureStorageConnectionString =
            @"[storage connection]";
        private string _containerName1 = $"appendtest{DateTime.Now:yyyyMMdd}";
        private string _containerName2 = "syncblobtest";
        private string _proxyUrl = null;

        public BlobServiceTest()
        {
            _service = new BlobService(new BlobConfigInfo()
            {
                AzureStorageConnectionString = _azureStorageConnectionString,
                ContainerName = _containerName1,
                ProxyUrl = _proxyUrl
            });
        }

        [TestMethod]
        [Description("Test CheckBlobContainer: Check current container exists or not")]
        public void CheckBlobContainerTest()
        {
            try
            {
                Assert.IsTrue(_service.CheckBlobContainer());
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                Assert.IsTrue(false);
            }
        }

        [TestMethod]
        [Description("Test CheckBlobFileExistsAsync: Check blob file exists or not")]
        public void CheckBlobFileExistsAsyncTest()
        {
            var fileName = $"test/{DateTime.Now:yyyy-MM-dd HH:mm:ss}_TestData.json";

            try
            {

                _service.CreateFileAsync(
                    fileName,
                    @"[testdata]",
                    null//new UnicodeEncoding()
                ).Wait();

                Assert.IsTrue(_service
                    .CheckBlobFileExistsAsync(fileName)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult()
                );
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                Assert.IsTrue(false);
            }
            finally
            {
                _service.DeleteFileAsync(fileName).Wait();
            }
        }

        [TestMethod]
        [Description("Test QueryFilesAsync: To query files by path hierarchy")]
        public void QueryFilesAsyncTest()
        {
            var folderName = $"Test/{Guid.NewGuid().ToString().Replace("-", "")}/";
            var fileNameList = new List<string>()
            {
                $"{folderName}TestData1.json",
                $"{folderName}TestData2.json",
                $"{folderName}TestData3.json"
            };

            try
            {
                foreach (var item in fileNameList)
                {
                    _service.CreateFileAsync(item,
                            @"[testdata]")
                        .Wait();
                }

                var list = _service.QueryFilesAsync($"{folderName}").Result;

                Assert.IsTrue(fileNameList.Count == list.Count());
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                Assert.IsTrue(false);
            }
            finally
            {
                foreach (var item in fileNameList)
                {
                    _service.DeleteFileAsync(item).Wait();
                }
            }
        }

        [TestMethod]
        [Description("Test CreateFileAsync: To upload block blob file with data")]
        public void CreateFileAsyncTest()
        {
            var fileName = $"test/{DateTime.Now:yyyy-MM-dd HH:mm:ss}_TestData.json";

            try
            {
                _service.CreateFileAsync(
                    fileName,
                    @"[testdata]",
                    null//new UnicodeEncoding()
                ).Wait();

                Assert.IsTrue(true);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                Assert.IsTrue(false);
            }
            finally
            {
                _service.DeleteFileAsync(fileName).Wait();
            }
        }

        [TestMethod]
        [Description("Test AppendFileAsync: To append data into specific file")]
        public void AppendFileAsyncTest()
        {
            var fileName = "test/TestData.json";

            try
            {
                _service.AppendFileAsync(
                    fileName,
                    @"testdata" + Environment.NewLine,
                    null//new UnicodeEncoding()
                ).Wait();
                _service.AppendFileAsync(
                    fileName,
                    @"testdata" + Environment.NewLine,
                    null//new UnicodeEncoding()
                ).Wait();

                Assert.IsTrue(true);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                Assert.IsTrue(false);
            }
            finally
            {
                _service.DeleteFileAsync(fileName).Wait();
            }
        }

        [TestMethod]
        [Description("Test DownloadAsStringAsync: To download file content as string")]
        public void DownloadAsStringAsyncTest()
        {
            var fileName = $"test/{DateTime.Now:yyyy-MM-dd HH:mm:ss}_TestData.json";

            try
            {
                _service.CreateFileAsync(fileName,
                        @"[testdata]")
                    .Wait();

                var content = _service.DownloadFileAsStringAsync(fileName).Result;

                Assert.IsTrue(!string.IsNullOrEmpty(content));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                Assert.IsTrue(false);
            }
            finally
            {
                _service.DeleteFileAsync(fileName).Wait();
            }
        }

        [TestMethod]
        [Description("Test DeleteFileAsync: To delete single file")]
        public void DeleteFileAsyncTest()
        {
            var fileName = $"test/{DateTime.Now:yyyy-MM-dd HH:mm:ss}_TestData.json";

            try
            {
                _service.CreateFileAsync(fileName, @"[testdata]").Wait();

                _service.DeleteFileAsync(fileName).Wait();

                Assert.IsTrue(true);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                Assert.IsTrue(false);
            }
        }

        [TestMethod]
        [Description("Test DeleteContainerIfExistsAsync: Delete specific container if exists")]
        public void DeleteContainerIfExistsAsyncTest()
        {
            var service = new BlobService(new BlobConfigInfo()
            {
                AzureStorageConnectionString = @"[storage connection]",
                ContainerName = @"deletecontainertest",
                ProxyUrl = _proxyUrl
            });

            try
            {
                service.DeleteContainerIfExistsAsync().Wait();

                Assert.IsTrue(true);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                Assert.IsTrue(false);
            }
        }

        [TestMethod]
        [Description("Test UploadFromFile: To upload single file")]
        public void UploadFromFileTest()
        {
            var localFilePath = @"D:\\test1.txt";
            var prefixPath = @"test/";
            var fileName = "test1.txt";

            try
            {
                File.WriteAllText(localFilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: test");

                _service.UploadFromFile(prefixPath, localFilePath, fileName);

                var content = _service.DownloadFileAsStringAsync(Path.Combine(prefixPath, fileName)).Result;

                Assert.IsTrue(!string.IsNullOrEmpty(content));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                Assert.IsTrue(false);
            }
            finally
            {
                _service.DeleteFileAsync(Path.Combine(prefixPath, fileName)).Wait();
            }
        }

        [TestMethod]
        [Description("Test DownloadFileAsync: To download file to local path")]
        public void DownloadFileAsyncTest()
        {
            var fileName = $"test/{DateTime.Now:yyyy-MM-dd HH:mm:ss}_TestData.json";
            var downloadPath = Path.Combine("D:\\Temp\\DownloadTest\\", Path.GetFileName(fileName));

            try
            {
                _service.CreateFileAsync(fileName,
                        @"[testdata]")
                    .Wait();

                _service.DownloadFileAsync(fileName, downloadPath, true)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                Assert.IsTrue(File.Exists(($"{Path.GetDirectoryName(downloadPath)}\\{Path.GetFileName(downloadPath)?.Replace("-", "").Replace("_", "").Replace(" ", "").Replace(":", "")}")));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                Assert.IsTrue(false);
            }
            finally
            {
                _service.DeleteFileAsync(fileName).Wait();
            }
        }

        [TestMethod]
        [Description("Test SyncSourceBlobAsync: To synchronize file(s) from current container to target container")]
        public void SyncSourceBlobAsyncTest()
        {
            var fileName = $"test/{DateTime.Now:yyyy-MM-dd HH:mm:ss}_TestData.json";

            try
            {
                _service.CreateFileAsync(fileName,
                        @"[testdata]")
                    .Wait();

                _service.SyncSourceBlobAsync(
                        new BlobConfigInfo()
                        {
                            AzureStorageConnectionString = _azureStorageConnectionString,
                            ContainerName = _containerName2,
                            ProxyUrl = _proxyUrl
                        },
                        fileName,
                        $"{Path.GetDirectoryName(fileName)}/",
                        $"{Path.GetDirectoryName(fileName)}Sync/"
                    )
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                Assert.IsTrue(true);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                Assert.IsTrue(false);
            }
            finally
            {
                _service.DeleteFileAsync(fileName).Wait();

                var service = new BlobService(new BlobConfigInfo()
                {
                    AzureStorageConnectionString = _azureStorageConnectionString,
                    ContainerName = _containerName2,
                    ProxyUrl = _proxyUrl
                });

                service.DeleteContainerIfExistsAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [Description("Test GetBlobFilesCount: To count files in specific path hierarchy")]
        public void GetBlobFilesCountTest()
        {
            var fileName = $"test/{DateTime.Now:yyyy-MM-dd HH:mm:ss}_TestData.json";

            try
            {
                _service.CreateFileAsync(fileName,
                        @"[testdata]")
                    .Wait();

                var count = _service.GetBlobFilesCount($"{Path.GetDirectoryName(fileName)}/");

                Assert.IsTrue(count > 0);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                Assert.IsTrue(false);
            }
            finally
            {
                _service.DeleteFileAsync(fileName).Wait();
            }
        }

        [TestMethod]
        [Description("Test GetContainerUri: Get current container's URI")]
        public void GetContainerUriTest()
        {
            try
            {
                var uri = _service.GetContainerUri();

                Assert.IsNotNull(uri);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                Assert.IsTrue(false);
            }
        }

        [TestMethod]
        [Description("Test GetBlobContainerSasToken: Get SAS token from current or specific container")]
        public void GetBlobContainerSasTokenTest()
        {
            try
            {
                var sasToken = _service.GetBlobContainerSasToken(
                    new BlobConfigInfo()
                    {
                        AzureStorageConnectionString = _azureStorageConnectionString,
                        ContainerName = _containerName2,
                        ProxyUrl = _proxyUrl
                    },
                    BlobContainerPermissions.All,
                    DateTime.UtcNow.AddHours(2),
                    false
                );

                Assert.IsTrue(!string.IsNullOrEmpty(sasToken), "Can't get SAS token");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                Assert.IsTrue(false);
            }
        }
    }
}
