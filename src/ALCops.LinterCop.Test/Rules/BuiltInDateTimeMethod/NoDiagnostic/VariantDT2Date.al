codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyVariant: Variant;
        MyDate: Date;
    begin
        MyDate := [|DT2Date(MyVariant)|];
    end;
}