
# Git HTTP Middleware

Middleware for adding Git HTTP services to ASP.NET core projects.

Allow for remote git commands such as `git clone`, `git push`, `git pull` and others.
## Setup
The path to the `git-upload-pack` and `git-receive-pack` binaries is required to be set in the appsettings. The path to the repositories directory can be set in the appsettings as well. If it is not set, the middleware will default to creating a repositories directory in the working directory of the application.

Here is an example appsettings configuration:

#### AppSettings
```json
"Git": {
    "git_binary_path": "path/to/binaries",
    "repositories_dir": "path/to/repositories"
  }
```
## Usage
Add a reference to the package in Programs.cs.

```csharp
using GitHTTPMiddleware;
```

Run the UseGitService middleware extenstion method on your WebApplication instance.

```csharp
WebApplication app = builder.Build();
app.UseGitService();
```

#### AppSettings

| Setting           | Example Value                                |
| ----------------- | -------------------------------------------- |
| git_binary_path   | /path/to/binaries                            |
| repositories_dir  | /path/to/repositories                        |
| authentication    | basic, token, windows authentication         |

Example

```json
"Git": {
    "git_binary_path": "/path/to/binaries",
    "repositories_dir": "/path/to/repositories",
    "authentication":  "basic"
  }
```


## License

[MIT](https://choosealicense.com/licenses/mit/)

