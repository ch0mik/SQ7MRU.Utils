# SQ7MRU.Utils
Utils to parse ADIF files and downloads the eQSLs

Nuget : [SQ7MRU.Utils](https://www.nuget.org/packages/SQ7MRU.Utils)

simple to use 

```c#
private static void Main(string[] args)
   {
    //Downloads Yours eQSL from eQSL.cc
    var eqsl = new Downloader("Yours Login To eQSL.cc", "password");
    eqsl.Download(); //Download ADIFs and e-QSLs 
	
    //Downloads iQSLs from hrdlog.net
    var hrd = new iQSL("Yours Login To hrdlog.net");
    hrd.Download();
    
    //Download certs from EPC (epc-mc.eu)
    var epc = new EPC("Yours Login To epc-mc.eu", "password");
    epc.Download();
    
    //Download certs from DMC (www.digital-modes-club.org)
    var dmc = new DMC("Yours Login To epc-mc.eu", "password");
    dmc.Download();
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
