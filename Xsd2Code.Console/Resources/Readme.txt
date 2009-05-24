-----------------------------------------------------------------------

Usage: 

	Xsd2Code.exe <XSD File> [Namespace] [Output file name] [Options]

Where:
    <XSD File>                          - Path to an XSD file. Required
    [Namespace]                         - Generated code namespace. Optional. File name without extension if no value is specified
    [Output file name]                  - Name of the output (generated) file. Optional. 
    [Options]                           - Optional. See below for description

Options:
    
    /i[nput] <FileName>                 - Name of the input (XSD) file
    /o[utput] <FileName>                - Name of the output (generated) file. 
                                          By default, name of the source file with extension .Designer.cs 
                                          (.Designer.vb or .Designer.cpp for VisualVasic and Visual C++ respectively)
    /n[s] <Namespace>                   - Generated code CLR namespace. Default: file name without extension
    /l[anguage] <Language>              - Generated code language (CS|VB|CPP). Default: CS
    /cb <Collection Base>               - Collection base (Array|List|ObservableCollection|DefinedType). Default: List
    /cu <Custom Usings>                 - Comma-separated of custom usings definition (E.g "Xsd2Code.Library,System.Xml.Linq")
    /sm <Serialize>                     - Serialize method name. Default: Serialize
    /dm <Deserialize>                   - Deserialize method name. Default: Deserialize
    /lf <LoadFromFile>                  - LoadFromFile method name. Default: LoadFromFile
    /sf <SaveToFile>                    - SaveToFile methodname. Default: SaveToFile
    /is[+]                              - Include Serialize method
    /is-                                - Do not include Serialize method (default)
    /cl[+]                              - Include Clone method
    /cl-                                - Do not include Clone method (default)
    /dbg[+]                             - Enable debug step through
    /dbg-                               - Disable debug step through (default)
    /db[+]                              - Enable data bindings (default)
    /db-                                - Disable data bindings
    /sc[+]                              - Enable summary comment
    /sc-                                - Disable summary comment (default)
    /hp[+]                              - Hide private fields in IDE (default)
    /hp-                                - Show private fields in IDE
    
    /lic[ense]                          - Show license information
    
    /?, /h[elp]                         - Show this help
    
Example:

	Xsd2Code.exe Employee.xsd CompanyXYZ.Entities.HumanResources Employee.cs /cb ObservableCollection /sc /dbg /cl /hp- /cu System.Xml.Linq,System.IO
	
	
	