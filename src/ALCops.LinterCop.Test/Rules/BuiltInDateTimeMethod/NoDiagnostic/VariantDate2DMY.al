codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyVariant: Variant;
        MyInteger: Integer;
    begin
        MyInteger := [|Date2DMY(MyVariant, 1)|];
    end;
}