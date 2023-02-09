# Explorer App
## This is a WebUI application for displaying the distribution of occupied disk space by directories and files.
![image](https://user-images.githubusercontent.com/100798944/217754873-57fe92ea-c651-438c-8a1f-14d338da525c.png)

The left side is a Tree View for hierarchically displaying the directory structure. Each node can be expanded to view subitems (if any) and collapsed to hide them.

![image](https://user-images.githubusercontent.com/100798944/217758126-f2b930f2-e132-4122-8f4e-115922013341.png)

The right side displays the contents of the selected directory. Sorting is performed by the occupied size of files and folders on the disk.

![image](https://user-images.githubusercontent.com/100798944/217756609-ffb87925-ce56-470c-bfe1-5c9583e97855.png)

## Before starting, you must specify the rootPath in the \Controllers\ExplorerController.cs file. Example:

```
string rootPath = @"C:\";
```
