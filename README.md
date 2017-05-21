# SQ7MRU.Utils
Utils to parse ADIF files and downloads the eQSLs

simple to use 

```c#
private static void Main(string[] args)
   {
    //Downloads Yours eQSL from eQSL.cc
    var d = new Downloader("YoursLoginTo_eQSL.cc", "password");
    d.GetAdifs(); //Downloads ADIF for Yours Account(s)
    d.DownloadJPGs(); //Download e-QSLs 
	
	//Downloads iQSLs from hrdlog.net
	var hrd = new iQSL("sq7mru");
    hrd.Download();
   }
```

[older project]( https://eqsldownloader.codeplex.com/)
