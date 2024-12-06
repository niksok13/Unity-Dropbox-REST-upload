# Unity-Dropbox-REST-upload
Unity tiny editor script to upload files to Dropbox via REST API

## Usage:
```cs
var dropbox = new RestDropbox(...)
var fileBytes = await File.ReadAllBytesAsync(path);
await dropbox.Upload(fileBytes, "build.apk")
Debug.Log("Upload finished")
```
