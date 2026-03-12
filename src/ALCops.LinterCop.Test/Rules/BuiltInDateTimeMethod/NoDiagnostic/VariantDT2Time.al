codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyVariant: Variant;
        MyTime: Time;
    begin
        MyTime := [|DT2Time(MyVariant)|];
    end;
}