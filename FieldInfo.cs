//  Copyright (C) 2023 - Present John Roscoe Hamilton - All Rights Reserved
//  You may use, distribute and modify this code under the terms of the MIT license.
//  See the file License.txt in the root folder for full license details.

namespace WFCodeGenLib;

// ClassInfo uses this class to store information for its fields and properties.
public class FieldInfo
{
    public string FullType = string.Empty;
    public string Type = string.Empty;
    public string Name = string.Empty;
    public string Initialization = string.Empty;
    public bool isPublic = false;
    public bool isPrivate => !isPublic;
    public bool isConst = false;
    public bool isStatic = false;
    public bool isReadonly = false;
    public bool isProperty = false;
    public bool isAutoProperty = false;
    public bool isKey = false;
    public bool isX = false;
    public bool isDataField = false;
    public bool isEnum = false;
    //public float Min = 0;
    //public float Max = 0;
    public FieldInfo()
    {
    }
}