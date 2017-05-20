# SQ7MRU.Utils
Utils to parse ADIF files and downloads the eQSLs

simple to use 

```c#
private static void Main(string[] args)
   {
    var d = new Downloader("YoursLoginTo_eQSL.cc", "password");
    d.GetAdifs(); //Downloads ADIF for Yours Account(s)
    d.DownloadJPGs(); //Download e-QSLs 
   }
```