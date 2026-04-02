page 50100 [|MyApiPage|]
{
    PageType = API;
    EntitySetName = 'myApiPages';
    EntityName = 'myApiPage';
    APIPublisher = 'myPublisher';
    APIGroup = 'myGroup';
    DelayedInsert = true;
    SourceTable = MyTable;

    layout
    {
        area(Content)
        {
            field([|MyField|]; Rec.MyField)
            {
            }
            group([|MyGroup|])
            {
                field([|MyVarField|]; MyVarField)
                {
                }
            }
        }
    }

    actions
    {
        area(Processing)
        {
            group([|MyActionGroup|])
            {
                action([|MyAction|])
                {
                }
            }
        }
    }

    var
        MyVarField: Text;
}

table 50100 MyTable { fields { field(1; MyField; Code[20]) { Caption = 'My Field'; } } }
