## Special Notice
Please make sure that one BlobService instance is handled for one blob container.
Otherwise, your data may be saved to incorrect blob container.

## Samples

- Initializing
```csharp
BlobService _service = new BlobService(new BlobConfigInfo()
    {
        AzureStorageConnectionString = @"[your storage account connection string]",
        BlobName = @"[your container name]"
    });
```

Or you can use DI to inject instance with IBlobService in Startup  
```csharp
builder.Services.AddSingleton<IBlobService, BlobService>(p =>
{
    var config = p.GetRequiredService<IOptions<ConfigModel>>().Value;

    return new BlobService(new BlobConfigInfo()
    {
        AzureStorageConnectionString = config.BaseInfo.StorageConnection,
        ContainerName = config.BaseInfo.ContainerName.ToLower()
    });
});
```

- CheckBlobContainer: Check current container exists or not
```csharp
_service.CheckBlobContainer()
```

- QueryFilesAsync: To query files by path hierarchy  
Note:  
Please follow the path hierarchy and end with '/'  

```csharp
var folderName = $"Test/{Guid.NewGuid().ToString().Replace("-", "")}/";
var list = _service.QueryFilesAsync($"{folderName}").Result;
```

- CreateFileAsync: To upload block blob file with data
```csharp
var fileName = $"test/{DateTime.Now:yyyy-MM-dd HH:mm:ss}_TestData.json";
_service.CreateFileAsync(
    fileName,
    @"testdata",
    null//new UnicodeEncoding()
).Wait();
```

- AppendFileAsync: To append data into specific blob file
```csharp
var fileName = "test/TestData.json";
_service.AppendFileAsync(
    fileName,
    @"testdata",
    null//new UnicodeEncoding()
).Wait();
_service.AppendFileAsync(
    fileName,
    @"testdata" + Environment.NewLine,
    null//new UnicodeEncoding()
).Wait();
```

- DownloadAsStringAsync: To download file content as string
```csharp
var fileName = $"test/{DateTime.Now:yyyy-MM-dd HH:mm:ss}_TestData.json";
_service.CreateFileAsync(fileName,
        @"testdata")
    .Wait();

var content = _service.DownloadFileAsStringAsync(fileName).Result;
```

- DeleteFileAsync: To delete single file
```csharp
var fileName = $"test/{DateTime.Now:yyyy-MM-dd HH:mm:ss}_TestData.json";
_service.CreateFileAsync(fileName, @"testdata").Wait();

_service.DeleteFileAsync(fileName).Wait();
```

- DeleteContainerIfExistsAsync: Delete specific container if exists
```csharp
_service.DeleteContainerIfExistsAsync().Wait();
```

- UploadFromFile: To upload single file
```csharp
var localFilePath = @"D:\\test1.txt";
var prefixPath = @"test/";
var fileName = "test1.txt";

_service.UploadFromFile(prefixPath, localFilePath, fileName);
```

- DownloadFileAsync: To download file to local path
```csharp
var fileName = $"test/{DateTime.Now:yyyy-MM-dd HH:mm:ss}_TestData.json";
var downloadPath = Path.Combine("D:\\Temp\\DownloadTest\\", Path.GetFileName(fileName));

_service.DownloadFileAsync(fileName, downloadPath, true)
  .ConfigureAwait(false)
  .GetAwaiter()
  .GetResult();
```

- DownloadFilesAsZip: To download files as single ZIP file
```csharp
var folderName = @"test/";
var zipFilePath = Path.Combine("D:\\Temp\\DownloadTest\\", "testZip.zip");

var list = _service.QueryFilesAsync(folderName).Result.ToList();

_service.DownloadFilesAsZip(
    list.Select(x => Path.Combine(x.BlobPath, x.FileName)),
    zipFilePath
    );
```

- UploadFilesAsZip: To upload files as single ZIP file
```csharp
var folderName = @"test/";
var targetPath = $"{folderName}{DateTime.Now:yyyyMMddHHmmssfff}_temp.zip";

var filesPath = "D:\\Temp\\DownloadTest\\";
var list = Directory.GetFiles(filesPath, "*", SearchOption.AllDirectories).ToList();

_service.UploadFilesAsZip(
    list.Select(Path.GetFullPath).ToList(),
    targetPath
);
```

- SyncSourceBlobAsync: To synchronize file(s) from current container to target container
```csharp
var fileName = $"test/{DateTime.Now:yyyy-MM-dd HH:mm:ss}_TestData.json";

_service.SyncSourceBlobAsync(
    new BlobConfigInfo()
    {
        AzureStorageConnectionString = "[Target storage account connection]",
        ContainerName = "[Target container name]"
    },
    fileName,
    $"{Path.GetDirectoryName(fileName)}/",
    $"{Path.GetDirectoryName(fileName)}Sync/"
)
  .ConfigureAwait(false)
  .GetAwaiter()
  .GetResult();
```

- GetBlobFilesCount: To count files in specific path hierarchy
```csharp
var fileName = $"test/{DateTime.Now:yyyy-MM-dd HH:mm:ss}_TestData.json";

var count = _service.GetBlobFilesCount($"{Path.GetDirectoryName(fileName)}/");
```

- GetContainerUri: Get current container's URI
```csharp
var uri = _service.GetContainerUri();
```

- GetBlobContainerSasToken: Get SAS token from current or specific container
```csharp
var sasToken = _service.GetBlobContainerSasToken(
    _azureStorageConnectionString,
    "testcontainer",
    BlobContainerPermissions.All,
    DateTime.UtcNow.AddHours(2),
    false
);
```