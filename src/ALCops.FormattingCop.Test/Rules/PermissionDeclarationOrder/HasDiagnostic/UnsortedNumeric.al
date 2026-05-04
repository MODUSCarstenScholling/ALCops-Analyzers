codeunit 50100 "Item 10"
{
}

codeunit 50101 "Item 2"
{
}

codeunit 50102 "Item 1"
{
}

permissionset 50100 "My Permission Set"
{
    Assignable = true;
    [|Permissions =
        codeunit "Item 1" = X,
        codeunit "Item 10" = X,
        codeunit "Item 2" = X|];
}
