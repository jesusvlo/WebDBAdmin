using FluentMigrator;
using FluentMigrator.Builders;
using FluentMigrator.Infrastructure;
using System;
using WebDBAdmin.Application.DTOs;

namespace WebDBAdmin.Infrastructure.Migrations;

[Migration(0)]
public class AdHocMigration : Migration
{
    private readonly Action<AdHocMigration> _upAction;

    public AdHocMigration(Action<AdHocMigration> upAction)
    {
        _upAction = upAction;
    }

    public override void Up()
    {
        _upAction(this);
    }

    public override void Down()
    {
        throw new NotImplementedException();
    }

    public void DefineColumn<T>(
        IColumnTypeSyntax<T> syntax,
        ColumnDefinition col) where T : IFluentSyntax
    {
        dynamic withType = ApplyTypeInternal(syntax, col.Type, col.Length);

        // Apply Constraints on the returned syntax object
        if (col.IsNullable)
            withType.Nullable();
        else
            withType.NotNullable();

        if (col.IsPrimaryKey)
        {
            withType.PrimaryKey();

            // Auto-apply Identity for numeric PKs
            if (IsNumericType(col.Type))
            {
                withType.Identity();
            }
        }
    }

    private bool IsNumericType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying == typeof(int) || underlying == typeof(long) ||
               underlying == typeof(short) || underlying == typeof(byte) ||
               underlying == typeof(uint) || underlying == typeof(ulong) ||
               underlying == typeof(ushort) || underlying == typeof(sbyte) ||
               underlying == typeof(decimal);
    }

    private dynamic ApplyTypeInternal<T>(
        IColumnTypeSyntax<T> syntax,
        Type type,
        int? length) where T : IFluentSyntax
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(int)) return syntax.AsInt32();
        if (underlying == typeof(uint)) return syntax.AsInt64();
        if (underlying == typeof(long)) return syntax.AsInt64();
        if (underlying == typeof(ulong)) return syntax.AsInt64();

        if (underlying == typeof(short)) return syntax.AsInt16();
        if (underlying == typeof(ushort)) return syntax.AsInt32();

        if (underlying == typeof(byte)) return syntax.AsByte();
        if (underlying == typeof(sbyte)) return syntax.AsInt16();

        if (underlying == typeof(bool)) return syntax.AsBoolean();

        if (underlying == typeof(decimal)) return syntax.AsDecimal();
        if (underlying == typeof(float)) return syntax.AsFloat();
        if (underlying == typeof(double)) return syntax.AsDouble();

        if (underlying == typeof(DateTime)) return syntax.AsDateTime();
        if (underlying == typeof(DateTimeOffset)) return syntax.AsDateTimeOffset();
        if (underlying == typeof(DateOnly)) return syntax.AsDate();
        if (underlying == typeof(TimeOnly)) return syntax.AsTime();
        if (underlying == typeof(TimeSpan)) return syntax.AsTime();

        if (underlying == typeof(Guid)) return syntax.AsGuid();

        if (underlying == typeof(byte[]))
            return length.HasValue ? syntax.AsBinary(length.Value) : syntax.AsBinary(int.MaxValue);

        if (underlying == typeof(string))
        {
            if (length.HasValue) return syntax.AsString(length.Value);
            return syntax.AsString(int.MaxValue);
        }

        if (underlying == typeof(char)) return syntax.AsFixedLengthString(1);

        return syntax.AsString();
    }
}
