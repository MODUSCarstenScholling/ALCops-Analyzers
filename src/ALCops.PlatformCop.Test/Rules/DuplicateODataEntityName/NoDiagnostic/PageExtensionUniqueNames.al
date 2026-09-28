// Extension controls have unique OData names relative to base page
page 50100 MyPage
{
    PageType = List;
    SourceTable = MyTable;

    layout
    {
        area(Content)
        {
            repeater(Lines)
            {
                field("Item No."; Rec.MyField) { ApplicationArea = All; }
            }
        }
    }
}

pageextension 50100 MyPageExt extends MyPage
{
    layout
    {
        addlast(Lines)
        {
            [|field("Item Description"; Rec.MyField2) { }|]
        }
    }
}

table 50100 MyTable
{
    fields
    {
        field(1; "Primary Key"; Integer) { }
        field(2; MyField; Integer) { }
        field(3; MyField2; Integer) { }
    }

    keys
    {
        key(PK; "Primary Key") { }
    }
}

table 5703 Location
{
    fields
    {
        field(1; "Primary Key"; Integer) { }
        field(2; "Cross-Dock Due Date Calc."; Integer) { }
    }

    keys
    {
        key(PK; "Primary Key") { }
    }
}

page 5703 "Location Card"
{
    Caption = 'Location Card';
    PageType = Card;
    SourceTable = Location;

    layout
    {
        area(content)
        {
            group(General)
            {
                Caption = 'General';

				field("Cross-Dock Due Date Calc."; Rec."Cross-Dock Due Date Calc.")
				{
					ApplicationArea = All;
				}
			}
		}
	}
}

tableextension 70101 "M365 ETSC Location" extends Location
{
    fields
    {
        field(70101; "Is Special Location"; Boolean)
        {
        }
    }
}


pageextension 70103 "M365 ETSC Location Card" extends "Location Card"
{
    layout
    {
        addafter("Cross-Dock Due Date Calc.")
        {
            field(IsSpecialLocation; Rec."Is Special Location")
            {
            }
        }
    }
}
