using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Sample.AzureStorageBlob.Blob.Enums;
using Sample.AzureStorageBlob.Blob.Models;

namespace Sample.AzureStorageBlob.Blob.Interfaces
{
    public interface IBlobService
    {
        /// <summary>
        /// Check current container exists or not
        /// </summary>
        /// <returns></returns>
        bool CheckBlobContainer();

        /// <summary>
        /// Check blob file exists or not
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        Task<bool> CheckBlobFileExistsAsync(string fileName);

        /// <summary>
        /// To query files by path hierarchy
        /// </summary>
        /// <param name="prefix">The searching prefix key word.</param>
        /// <returns></returns>
        Task<IEnumerable<BlobInfo>> QueryFilesAsync(string prefix);

        /// <summary>
        /// To upload block blob file with data
        /// </summary>
        /// <param name="fileName">The Azure blob file path.</param>
        /// <param name="jsonData">File content.</param>
        /// <param name="encoding"></param>
        /// <param name="isWriteBomBytes"></param>
        /// <returns></returns>
        Task CreateFileAsync(
            string fileName,
            string content,
            Encoding encoding = null,
            bool isWriteBomBytes = false
        );

        /// <summary>
        /// To append data into specific file
        /// </summary>
        /// <param name="relativeFilePath"></param>
        /// <param name="data"></param>
        /// <param name="encoding"></param>
        /// <param name="isWriteBomBytes"></param>
        /// <returns></returns>
        Task AppendFileAsync(
            string relativeFilePath,
            string content,
            Encoding encoding = null,
            bool isWriteBomBytes = false
        );

        /// <summary>
        /// To download file content as string
        /// </summary>
        /// <param name="fileName">The Azure blob file path.</param>
        /// <returns></returns>
        Task<string> DownloadFileAsStringAsync(
            string fileName
        );

        /// <summary>
        /// To delete single file
        /// </summary>
        /// <param name="fileName">The Azure blob file path.</param>
        /// <returns></returns>
        Task DeleteFileAsync(
            string fileName
        );

        /// <summary>
        /// Delete specific container if exists
        /// </summary>
        /// <returns></returns>
        Task DeleteContainerIfExistsAsync();

        /// <summary>
        /// To upload single file
        /// </summary>
        /// <param name="prefixPath">The Azure blob prefix path.</param>
        /// <param name="localFileFullPath">The local file path.</param>
        /// <param name="blobPath">The Azure blob target path.</param>
        void UploadFromFile(
            string prefixPath,
            string localFileFullPath,
            string blobPath
        );

        /// <summary>
        /// To download file to local path
        /// </summary>
        /// <param name="blobPath"></param>
        /// <param name="saveAsPath">Path to save on local drive</param>
        /// <param name="printInfo">Flag for print downloading information</param>
        Task DownloadFileAsync(
            string blobPath,
            string saveAsPath,
            bool printInfo = false
        );

        /// <summary>
        /// To synchronize file(s) from current container to target container
        /// </summary>
        /// <param name="targetBlobInfo">Destination blob information</param>
        /// <param name="sourceBlobPath">Source file path</param>
        /// <param name="prefixSource">Prefix of source</param>
        /// <param name="prefixTarget">Prefix of destination</param>
        /// <param name="printConsole">Export information to console</param>
        Task SyncSourceBlobAsync(
            BlobConfigInfo targetBlobInfo,
            string sourceBlobPath,
            string prefixSource = "",
            string prefixTarget = "",
            bool printConsole = false
        );

        /// <summary>
        /// To count files in specific path hierarchy
        /// </summary>
        /// <param name="prefix">virtual path</param>
        /// <returns>files count</returns>
        int GetBlobFilesCount(string prefix);

        /// <summary>
        /// Get current container's URI
        /// </summary>
        /// <returns></returns>
        Uri GetContainerUri();

        /// <summary>
        /// Get SAS token from current or specific container
        /// </summary>
        /// <param name="targetBlobInfo"></param>
        /// <param name="permission"></param>
        /// <param name="expiryTime"></param>
        /// <param name="isUseCurrentBlobContainer"></param>
        /// <returns></returns>
        string GetBlobContainerSasToken(
            BlobConfigInfo targetBlobInfo,
            BlobContainerPermissions permission,
            DateTimeOffset expiryTime,
            bool isUseCurrentBlobContainer = true
        );
    }
}
