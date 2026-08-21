codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        CarriageReturn: Char;
        LineFeed: Char;
        TypeHelper: Codeunit "Type Helper";
    begin
        CarriageReturn := TypeHelper.CRLFSeparator() [1];
        LineFeed := TypeHelper.CRLFSeparator() [2];
    end;
}