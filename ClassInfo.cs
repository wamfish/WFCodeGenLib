//  Copyright (C) 2023 - Present John Roscoe Hamilton - All Rights Reserved
//  You may use, distribute and modify this code under the terms of the MIT license.
//  See the file License.txt in the root folder for full license details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace WFCodeGenLib;

/// <summary>
/// Gather information for a class based on it's ClassDeclarationSyntax.
/// </summary>
public class ClassInfo
{
    public static INamedTypeSymbol KAttribute;
    public static INamedTypeSymbol XAttribute;
    public static INamedTypeSymbol RangeAttribute;
    public string Name { get; set; } = string.Empty;
    public string BaseClass { get; set; } = string.Empty;
    public string SourceFilePath { get; set; }
    public bool IsData => BaseClass == "Data";
    public bool IsRecord => !IsData;
    public string Namespace { get; set; } = string.Empty;
    public string Initializer { get; set; } = string.Empty;
    public List<string> Comments { get; set; } = new List<string>();
    public bool isPublic = false;
    public bool isPrivate => !isPublic;
    public bool isSealed = false;
    public bool isStatic = false;
    public bool isPartial = false;
    public List<FieldInfo> Fields = new List<FieldInfo>();
    public ClassInfo(Compilation comp, ClassDeclarationSyntax cds)
    {
        List<ISymbol> fields = new List<ISymbol>();
        //List<IFieldSymbol> fields = new List<IFieldSymbol>();
        //List<IPropertySymbol> properties = new List<IPropertySymbol>();
        Name = cds.Identifier.ToString();
        var cta = cds.ChildTokens();
        foreach (var ct in cta)
        {
            if (ct.ValueText == "partial") isPartial = true;
        }
        var model = comp.GetSemanticModel(cds.SyntaxTree);
        var symbol = model.GetDeclaredSymbol(cds);
        Namespace = symbol.ContainingNamespace.ToString();

        if (!(symbol is ITypeSymbol)) return;

        var type = symbol as ITypeSymbol;

        var bt = type.BaseType;
        if (bt != null)
        {
            BaseClass = bt.Name;
            var bmembers = bt.GetMembers();
            foreach (var member in bmembers)
            {
                if (member is IFieldSymbol)
                {
                    fields.Add(member);
                    continue;
                }
                if (member is IPropertySymbol)
                {
                    fields.Add(member);
                    continue;
                }
            }
            //fields.AddRange(bt.GetMembers().OfType<IFieldSymbol>());
            //properties.AddRange(bt.GetMembers().OfType<IPropertySymbol>());
        }
        //var fieldTypes = type.GetMembers();
        //foreach (var fieldType in fieldTypes)
        //{
        //    Comments.Add($"Field: {fieldType.MetadataName} {fieldType.GetType().Name}");
        //}
        var members = type.GetMembers();
        foreach (var member in members)
        {
            if (member is IFieldSymbol)
            {
                fields.Add(member);
                continue;
            }
            if (member is IPropertySymbol)
            {
                fields.Add(member);
                continue;
            }
        }

        //fields.AddRange(type.GetMembers().OfType<IFieldSymbol>());
        //properties.AddRange(type.GetMembers().OfType<IPropertySymbol>());

        if (symbol.DeclaredAccessibility == Accessibility.Public) isPublic = true;
        if (symbol.IsSealed) isSealed = true;
        if (symbol.IsStatic) isStatic = true;
        //var bc = classes.Where(x => x.Identifier.ValueText == BaseClass);
        //if (bc.Count() > 0) 
        //    LoadFields(comp,bc.First());
        //LoadFields(comp,cds); 
        //LoadFields(comp, cds, fields);
        //LoadProperties(comp,properties);


        foreach (var field in fields)
        {
            var fi = new FieldInfo();
            fi.Name = field.Name;
            if (field is IFieldSymbol fs)
            {
                Fields.Add(fi);
                if (fs.Type.TypeKind == TypeKind.Enum)
                {
                    fi.isEnum = true;
                    //fi.EnumType = ps.Type.Name;
                }
                fi.FullType = FullType(fs.Type.ToString());
                fi.Type = RemoveNamespaces(fs.Type.ToString());
                if (fi.Type.StartsWith("DataField")) fi.isDataField = true;
                if (fs.DeclaredAccessibility == Accessibility.Public) fi.isPublic = true;
                if (fs.IsConst) fi.isConst = true;
                if (fs.IsStatic) fi.isStatic = true;
                if (fs.IsReadOnly) fi.isReadonly = true;
                if (!fs.DeclaringSyntaxReferences.IsEmpty)
                {
                    var vsr = fs.DeclaringSyntaxReferences.First();
                    var vs = vsr.GetSyntax();
                    if (vs is VariableDeclaratorSyntax vds)
                    {
                        if (vds.Initializer != null)
                            fi.Initialization = vds.Initializer.ToFullString();
                        var vmodel = comp.GetSemanticModel(vds.SyntaxTree);
                        var fsymbol = vmodel.GetDeclaredSymbol(vds);
                        if (fsymbol != null)
                        {
                            var attribs = fsymbol.GetAttributes();
                            for (int i = 0; i < attribs.Length; i++)
                            {
                                var ad = attribs[i];
                                if (ad.AttributeClass.Equals(KAttribute, SymbolEqualityComparer.Default))
                                {
                                    fi.isKey = true;
                                }
                                if (ad.AttributeClass.Equals(XAttribute, SymbolEqualityComparer.Default))
                                {
                                    fi.isX = true;
                                }
                                //if (ad.AttributeClass.Equals(DataGenerator.RangeAttribute, SymbolEqualityComparer.Default))
                                //{
                                //    ImmutableArray<TypedConstant> args = ad.ConstructorArguments;
                                //    fi.Min = (float) args[0].Value;
                                //    fi.Max = (float) args[1].Value;
                                //    //foreach (KeyValuePair<string, TypedConstant> namedArgument in ad.NamedArguments)
                                //    //{
                                //    //    if (namedArgument.Key == "Min")
                                //    //    {
                                //    //        var min = namedArgument.Value.Value;
                                //    //        fi.Min = (float) min;
                                //    //    }
                                //    //    if (namedArgument.Key == "Max")
                                //    //    {
                                //    //        var min = namedArgument.Value.Value;
                                //    //        fi.Min = (float)min;
                                //    //    }
                                //    //}
                                //}
                            }
                        }
                    }
                }
            }
            if (field is IPropertySymbol ps)
            {
                Fields.Add(fi);
                if (ps.Type.TypeKind == TypeKind.Enum)
                {
                    fi.isEnum = true;
                    //fi.EnumType = ps.Type.Name;
                }
                fi.FullType = FullType(ps.Type.ToString());
                fi.Type = RemoveNamespaces(ps.Type.ToString());
                fi.isProperty = true;
                if (fi.Type.StartsWith("DataField")) fi.isDataField = true;
                if (ps.DeclaredAccessibility == Accessibility.Public) fi.isPublic = true;
                //if (ps.IsConst) fi.isConst = true;
                if (ps.IsStatic) fi.isStatic = true;
                if (ps.IsReadOnly) fi.isReadonly = true;
                if (!ps.DeclaringSyntaxReferences.IsEmpty)
                {
                    var vsr = ps.DeclaringSyntaxReferences.First();
                    var vs = vsr.GetSyntax();
                    if (vs is PropertyDeclarationSyntax vds)
                    {
                        if (vds.Initializer != null)
                            fi.Initialization = vds.Initializer.ToFullString();
                        var vmodel = comp.GetSemanticModel(vds.SyntaxTree);
                        var fsymbol = vmodel.GetDeclaredSymbol(vds);
                        if (fsymbol != null)
                        {
                            var attribs = fsymbol.GetAttributes();
                            for (int i = 0; i < attribs.Length; i++)
                            {
                                var ad = attribs[i];
                                if (ad.AttributeClass.Equals(KAttribute, SymbolEqualityComparer.Default))
                                {
                                    fi.isKey = true;
                                }
                                if (ad.AttributeClass.Equals(XAttribute, SymbolEqualityComparer.Default))
                                {
                                    fi.isX = true;
                                }
                                //if (ad.AttributeClass.Equals(DataGenerator.RangeAttribute, SymbolEqualityComparer.Default))
                                //{
                                //    ImmutableArray<TypedConstant> args = ad.ConstructorArguments;
                                //    fi.Min = (float) args[0].Value;
                                //    fi.Max = (float) args[1].Value;
                                //    //foreach (KeyValuePair<string, TypedConstant> namedArgument in ad.NamedArguments)
                                //    //{
                                //    //    if (namedArgument.Key == "Min")
                                //    //    {
                                //    //        var min = namedArgument.Value.Value;
                                //    //        fi.Min = (float) min;
                                //    //    }
                                //    //    if (namedArgument.Key == "Max")
                                //    //    {
                                //    //        var min = namedArgument.Value.Value;
                                //    //        fi.Min = (float)min;
                                //    //    }
                                //    //}
                                //}
                            }
                        }
                    }
                }
            }
        }
    }
    public string FullType(string type)
    {
        return type.Replace("WFCodeGen.", "");
    }
    public string RemoveNamespaces(string type)
    {
        //currently I only handle Generic Types with 1 argument
        var sb = new StringBuilder();

        var tspan = type.AsSpan();

        bool inArg = false;
        int typeNamespaceEnd = 0;
        int argNamespaceStart = 0;
        int argNamespaceEnd = 0;

        for (int i = 0; i < tspan.Length; i++)
        {
            var c = tspan[i];
            if (c == '.')
            {
                if (inArg)
                {
                    argNamespaceEnd = i;
                    continue;
                }
                typeNamespaceEnd = i;
                continue;
            }
            if (c == '<')
            {
                if (inArg) continue; //we dont handle generic arguments yet
                argNamespaceStart = i + 1;
                argNamespaceEnd = i + 1;
                inArg = true;
                continue;
            }
        }
        if (argNamespaceStart == argNamespaceEnd)
        {
            argNamespaceStart = int.MaxValue;
            argNamespaceEnd = int.MaxValue;
        }
        for (int i = 0; i < tspan.Length; i++)
        {
            var c = tspan[i];
            if (c == '.') continue;
            if (i >= typeNamespaceEnd && i < argNamespaceStart)
            {
                sb.Append(c);
                continue;
            }
            if (i >= argNamespaceEnd)
            {
                sb.Append(c);
                continue;
            }
        }
        return sb.ToString();
        //if (nameSpace.Length > 0)
        //{
        //    type = type.Replace(nameSpace + ".", "");
        //}
        //type = type.Replace("WFGodot.", "");
        //type = type.Replace("Godot.", "");
        //type = type.Replace("System.", "");
        //return type;
    }
    //private void LoadFields(Compilation comp, ClassDeclarationSyntax cds, List<IFieldSymbol> fields)
    //{
    //    //var model = comp.GetSemanticModel(cds.SyntaxTree);
    //    foreach (var field in fields)
    //    {
    //        var fi = new FieldInfo();
    //        fi.Name = field.Name;
    //        fi.FullType = field.Type.ToString();
    //        fi.Type = RemoveNamespaces(field.Type.ToString());
    //        if (fi.Type.StartsWith("DataField")) fi.isDataField = true;

    //        if (field.DeclaredAccessibility == Accessibility.Public) fi.isPublic = true;
    //        if (field.IsConst) fi.isConst = true;
    //        if (field.IsStatic) fi.isStatic = true;
    //        if (field.IsReadOnly) fi.isReadonly = true;
    //        if (!field.DeclaringSyntaxReferences.IsEmpty)
    //        {
    //            var vsr = field.DeclaringSyntaxReferences.First();
    //            var vs = vsr.GetSyntax();
    //            if (vs is VariableDeclaratorSyntax vds)
    //            {
    //                if (vds.Initializer != null)
    //                    fi.Initialization = vds.Initializer.ToFullString();
    //                var model = comp.GetSemanticModel(vds.SyntaxTree);
    //                var fsymbol = model.GetDeclaredSymbol(vds);
    //                if (fsymbol != null)
    //                {
    //                    var attribs = fsymbol.GetAttributes();
    //                    for (int i = 0; i < attribs.Length; i++)
    //                    {
    //                        var ad = attribs[i];
    //                        if (ad.AttributeClass.Equals(KAttribute, SymbolEqualityComparer.Default))
    //                        {
    //                            fi.isKey = true;
    //                        }
    //                        if (ad.AttributeClass.Equals(XAttribute, SymbolEqualityComparer.Default))
    //                        {
    //                            fi.isX = true;
    //                        }
    //                        //if (ad.AttributeClass.Equals(DataGenerator.RangeAttribute, SymbolEqualityComparer.Default))
    //                        //{
    //                        //    ImmutableArray<TypedConstant> args = ad.ConstructorArguments;
    //                        //    fi.Min = (float) args[0].Value;
    //                        //    fi.Max = (float) args[1].Value;
    //                        //    //foreach (KeyValuePair<string, TypedConstant> namedArgument in ad.NamedArguments)
    //                        //    //{
    //                        //    //    if (namedArgument.Key == "Min")
    //                        //    //    {
    //                        //    //        var min = namedArgument.Value.Value;
    //                        //    //        fi.Min = (float) min;
    //                        //    //    }
    //                        //    //    if (namedArgument.Key == "Max")
    //                        //    //    {
    //                        //    //        var min = namedArgument.Value.Value;
    //                        //    //        fi.Min = (float)min;
    //                        //    //    }
    //                        //    //}
    //                        //}
    //                    }
    //                }
    //            }
    //        }
    //        Fields.Add(fi);
    //    }
    //}
    //private void LoadProperties(Compilation comp, List<IPropertySymbol> properties)
    //{
    //    foreach (var field in properties)
    //    {
    //        var fi = new FieldInfo();
    //        fi.isProperty = true;
    //        fi.Name = field.Name;
    //        if (field.DeclaredAccessibility == Accessibility.Public) fi.isPublic = true;
    //        if (field.IsStatic) fi.isStatic = true;
    //        if (field.IsReadOnly) fi.isReadonly = true;
    //        if (field.DeclaringSyntaxReferences.Count() > 0)
    //        {
    //            foreach (var s in field.DeclaringSyntaxReferences)
    //            {
    //                var ds = s.GetSyntax();
    //                if (ds is PropertyDeclarationSyntax pds)
    //                {
    //                    if (pds.Initializer != null) fi.Initialization = pds.Initializer.ToFullString();
    //                    var model = comp.GetSemanticModel(pds.SyntaxTree);
    //                    var fsymbol = model.GetDeclaredSymbol(pds);
    //                    fi.FullType = pds.Type.ToString();
    //                    fi.Type = RemoveNamespaces(pds.Type.ToString());
    //                    if (fi.Type.StartsWith("DataField")) fi.isDataField = true;
    //                    if (fsymbol != null)
    //                    {
    //                        fi.isAutoProperty = IsAutoProperty(fsymbol);
    //                        var attribs = fsymbol.GetAttributes();
    //                        for (int i = 0; i < attribs.Length; i++)
    //                        {
    //                            var ad = attribs[i];
    //                            if (ad.AttributeClass.Equals(KAttribute, SymbolEqualityComparer.Default))
    //                            {
    //                                fi.isKey = true;
    //                            }
    //                            if (ad.AttributeClass.Equals(XAttribute, SymbolEqualityComparer.Default))
    //                            {
    //                                fi.isX = true;
    //                            }
    //                            //if (ad.AttributeClass.Equals(DataGenerator.RangeAttribute, SymbolEqualityComparer.Default))
    //                            //{
    //                            //    ImmutableArray<TypedConstant> args = ad.ConstructorArguments;
    //                            //    fi.Min = (float)args[0].Value;
    //                            //    fi.Max = (float)args[1].Value;
    //                            //    //foreach (KeyValuePair<string, TypedConstant> namedArgument in ad.NamedArguments)
    //                            //    //{
    //                            //    //    if (namedArgument.Key == "Min")
    //                            //    //    {
    //                            //    //        var min = namedArgument.Value.Value;
    //                            //    //        fi.Min = (float)min;
    //                            //    //    }
    //                            //    //    if (namedArgument.Key == "Max")
    //                            //    //    {
    //                            //    //        var min = namedArgument.Value.Value;
    //                            //    //        fi.Min = (float)min;
    //                            //    //    }
    //                            //    //}
    //                            //}
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //        Fields.Add(fi);
    //    }
    //}
    static bool IsAutoProperty(IPropertySymbol propertySymbol)
    {
        // Get fields declared in the same type as the property
        var fields = propertySymbol.ContainingType.GetMembers().OfType<IFieldSymbol>();

        // Check if one field is associated to
        return fields.Any(field => SymbolEqualityComparer.Default.Equals(field.AssociatedSymbol, propertySymbol));
    }

    public void GetFields(out List<FieldInfo> fields, out List<FieldInfo> xfields)
    {
        fields = new List<FieldInfo>();
        xfields = new List<FieldInfo>();
        foreach (var f in Fields)
        {
            if (f.isStatic) continue;
            if (f.isConst) continue;
            if (f.isX)
            {
                xfields.Add(f);
                continue;
            }
            if (f.isPrivate) continue;
            if (f.isReadonly) continue;
            if (f.Type.StartsWith("List"))
            {
                //Note: There is still code that supports List, but it is not being generated. I
                // have not removed the code because I may want to add it back in later. I don't
                // see this happening, but I am leaving it in for now.

                // I decided to remove List support in favor of adding List support to the
                // DataField field type. The logic being that a DataField will be easy to add UI support for, as they
                // are pretty much identical to a Record. By removing regular list simplifies the code base by a ton.

                throw new Exception($"{Name}: I removed List from the supported types, use DataField instead");
                //continue;
            }
            //if (f.isProperty) continue;
            fields.Add(f);
        }
    }
}