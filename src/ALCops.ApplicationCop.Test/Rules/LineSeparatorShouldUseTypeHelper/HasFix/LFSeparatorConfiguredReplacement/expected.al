codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyChar: Char;
        typeHelper: Codeunit "My Type Helper";
    begin
        MyChar := typeHelper.GetLfSeparator();
    end;
}