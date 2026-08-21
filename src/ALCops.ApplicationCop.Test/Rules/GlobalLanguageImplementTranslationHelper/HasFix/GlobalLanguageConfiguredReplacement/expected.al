codeunit 50100 MyCodeunit
{
    procedure MyProcedure()
    var
        MyTranslationHelper: Codeunit "My Translation Helper";
    begin
        MyTranslationHelper.SetLanguageById(1);
    end;
}
