codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyVariant: Variant;
        MyInteger: Integer;
    begin
        MyInteger := [|Date2DWY(MyVariant, 1)|];
    end;
}