using System;
using System.Collections.Generic;
using System.Linq;
using UtilityExtensions;
using System.Text;
using System.Xml.Linq;
using System.Linq.Expressions;

namespace CmsData
{
    public enum FieldType
    {
        Empty,
        String,
        Number,
        NumberSimple,
        Integer,
        Date,
        DateSimple,
        Bit,
        NullBit,
        Code,
        CodeStr,
        DateField,
        NullNumber,
        NullInteger,
        Group,
        StringEqual,
        IntegerEqual,
        IntegerSimple,
    }
}
