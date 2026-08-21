codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyText: Text;
    begin
        [|MyText[1] := 13;|]
        MyText[2] := 10;
    end;
}