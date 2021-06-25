using System;

namespace Sample.AzureStorageBlob.Blob.Models
{
    public class BlobInfo
    {
        public string Uri { get; set; }
        public string BlobType { get; set; }
        public string FileName { get; set; }
        public string BlobPath { get; set; }
        public DateTime? CreatedTime { get; set; }
        public DateTime? LastModifiedTime { get; set; }
    }
}
