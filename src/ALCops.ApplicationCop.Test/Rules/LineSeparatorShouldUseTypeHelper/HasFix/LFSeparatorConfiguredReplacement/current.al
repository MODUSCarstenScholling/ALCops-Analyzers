codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyChar: Char;
    begin
        [|MyChar := 10;|]
    end;
}