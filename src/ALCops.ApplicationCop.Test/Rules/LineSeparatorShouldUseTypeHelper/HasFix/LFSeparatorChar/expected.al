codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyChar: Char;
        TypeHelper: Codeunit "Type Helper";
    begin
        MyChar := TypeHelper.LFSeparator();
    end;
}