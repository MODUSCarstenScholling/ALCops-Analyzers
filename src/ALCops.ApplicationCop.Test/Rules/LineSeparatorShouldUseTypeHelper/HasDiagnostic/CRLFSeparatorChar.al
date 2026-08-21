codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        CarriageReturn: Char;
        LineFeed: Char;
    begin
        [|CarriageReturn := 13;|]
        LineFeed := 10;
    end;
}