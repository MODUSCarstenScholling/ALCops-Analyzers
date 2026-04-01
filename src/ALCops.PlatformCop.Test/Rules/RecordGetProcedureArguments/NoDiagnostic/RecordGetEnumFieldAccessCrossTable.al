codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTable: Record MyTable;
        MyOtherTable: Record MyOtherTable;
    begin
        [|MyTable.Get(MyOtherTable.MyField::MyValue)|];
    end;
}

table 50100 MyTable { fields { field(1; MyField; Enum MyEnum) { } } }
table 50101 MyOtherTable { fields { field(1; MyField; Enum MyEnum) { } } }
enum 50100 MyEnum { value(0; MyValue) { } }
