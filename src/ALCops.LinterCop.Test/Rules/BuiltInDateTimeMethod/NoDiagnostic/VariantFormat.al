codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyVariant: Variant;
        MyInteger: Integer;
    begin
        Evaluate(MyInteger, [|Format(MyVariant, 2, '<HOURS24>')|]);
    end;
}