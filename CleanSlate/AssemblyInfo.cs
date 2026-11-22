using System.Runtime.CompilerServices;
using System.Windows;

// Autorise le projet de tests (CleanSlate.Tests) à utiliser les éléments « internal » de l'appli.
[assembly: InternalsVisibleTo("CleanSlate.Tests")]

[assembly:ThemeInfo(
    ResourceDictionaryLocation.None,            //where theme specific resource dictionaries are located
                                                //(used if a resource is not found in the page,
                                                // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly   //where the generic resource dictionary is located
                                                //(used if a resource is not found in the page,
                                                // app, or any theme specific resource dictionaries)
)]
