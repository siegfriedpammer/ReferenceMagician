ReferenceMagician
=================

This utility allows you to package an assembly and its transitive references into one directory.

```
Usage: referencemagician [OPTIONS]+ Assembly+

Options:
  -h, --help                 show this message and exit
  -o, --out, --output=VALUE  output directory for all assemblies, uses 'lib'
                               if not specified
      --nogac                if specified, GAC (Global Assembly Cache)
                               assemblies are ignored
      --includebcl, --bcl    if specified, BCL assemblies are included
```

License
-------

ReferenceMagician is provided under the terms of the MIT license. See license.txt for details.
It uses portions of Mono.Cecil (Thanks to Jb Evain), ILSpy (thanks to ic#code) and NDesk.Options (see http://ndesk.org/Options).