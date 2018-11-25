# SQ7MRU.Utils
Utils to parse ADIF files and downloads the eQSLs

Nuget : [SQ7MRU.Utils](https://www.nuget.org/packages/SQ7MRU.Utils)

simple to use 

```c#
private static void Main(string[] args)
   {
    //Downloads Yours eQSL from eQSL.cc
    var eqsl = new Downloader("YoursLoginTo_eQSL.cc", "password");
    eqsl.Download(); //Download ADIFs and e-QSLs 
	
    //Downloads iQSLs from hrdlog.net
    var hrd = new iQSL("sq7mru");
    hrd.Download();
    
    //Download certs from EPC (epc-mc.eu)
    var epc = new EPC(login, password);
    epc.Download();
   }
```

OR

```
git clone https://github.com/ch0mik/SQ7MRU.Utils.git
cd SQ7MRU.Utils
cd SampleApp
dotnet run
```

[older project]( https://eqsldownloader.codeplex.com/)
