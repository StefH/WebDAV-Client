# WebDAV-Client
An easy-to-use async WebDAV client for .NETStandard, Portable and .NET based on https://github.com/skazantsev/WebDavClient.

**Basic usage:**
``` csharp
using (var webDavClient = new WebDavClient())
{
    var result = await webDavClient.Propfind("http://mywebdav/1.txt");
    if (result.IsSuccessful)
        // continue ...
    else
        // handle an error
}
```

**Using BaseAddress:**
``` csharp
var clientParams = new WebDavClientParams { BaseAddress = new Uri("http://mywebdav/") };
using (var webDavClient = new WebDavClient(clientParams))
{
    await webDavClient.Propfind("1.txt");
}
```

**Operations with files and directories (resources & collections):**
``` csharp
var clientParams = new WebDavClientParams { BaseAddress = new Uri("http://mywebdav/") };
using (var webDavClient = new WebDavClient(clientParams))
{
    await webDavClient.Mkcol("mydir"); // create a directory

    await webDavClient.Copy("source.txt", "dest.txt"); // copy a file

    await webDavClient.Move("source.txt", "dest.txt"); // move a file

    await webDavClient.Delete("file.txt", "dest.txt"); // delete a file

    await webDavClient.GetRawFile("file.txt"); // get a file without processing from the server

    await webDavClient.GetProcessedFile("file.txt"); // get a file that can be processed by the server

    await webDavClient.PutFile("file.xml", File.OpenRead("file.xml"), "text/xml"); // upload a resource
}
```

**PROPFIND example:**
``` csharp
// list files & subdirectories in 'mydir'
var result = await webDavClient.Propfind("http://mywebdav/mydir");
if (result.IsSuccessful)
{
    foreach (var res in result.Resources)
    {
        Trace.WriteLine("Name: " + res.DisplayName);
        Trace.WriteLine("Is directory: " + res.IsCollection);
        // other params
    }
}
```

**Authentication example:**
``` csharp
var clientParams = new WebDavClientParams
{
    BaseAddress = new Uri("http://mywebdav/"),
    Credentials = new NetworkCredential("user", "12345")
};
using (var webDavClient = new WebDavClient(clientParams))
{
    // call webdav methods...
}
```

##License
WebDAVClient is licensed under the MIT License. See [LICENSE.txt](https://github.com/stefh/WebDAV-Client/blob/master/LICENSE.txt) for more details.