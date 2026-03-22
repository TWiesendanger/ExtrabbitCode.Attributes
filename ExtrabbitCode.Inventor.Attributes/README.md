# How to use this template

This is a Template for writting dotnet core based Inventor Addins. Version 2025 and higher. The complete documentation can be found here: [ExtrabbitCode Inventor Core Template Documentation](https://github.com/TWiesendanger/ExtrabbitCode.Inventor.Attributes)

A short overview is contained in this readme.

## Install

Use this command in your terminal:

`dotnet new install ExtrabbitCode.Inventor.Attributes@1.0.1`

## Change Inventor Version

To change the inventor version, you need to edit the project file. Look for `<InventorVersion>$(InventorVersion)</InventorVersion>` and change it to the desired version. Even better you look for the `Directory.Build.props` file in the root folder and change it there. This way all projects will use the same version.

### Use iLogic

To use iLogic dll's, you need to edit the project file. You can add `<UseILogic>true</UseILogic>`.

## Analyzer

The analyzer is pretty strict on purpose. It forces you to use best practises and avoid common mistakes. If it is to strict for you, feel free to edit the ruleset file called `.editorconfig` in the root folder. By default using `var` is not allowed. If you want to change this, just edit the file and change the corresponding rule.

Also `Warnings` are treated as errors. If you want to change this, you can do this by removing this line from the project file:

```xml
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
```
