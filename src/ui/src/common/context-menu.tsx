import React from "react";
import { DropDownMenu } from "./drop-down-menu";

import { List, ListActionEvent, ListItem, ListItemGraphic, ListItemText } from "@episerver/ui-framework";
import MaterialIcon from "@material/react-material-icon";

interface MenuItem {
    name: string;
    icon?: string;
    onSelected: () => void;
}

interface ContextMenuProps {
    title: string;
    icon: string;
    menuItems?: MenuItem[];
}

export class ContextMenu extends React.Component<ContextMenuProps, any> {
    dropDownMenu: DropDownMenu;

    constructor(props: ContextMenuProps) {
        super(props);
        this.state = {
            title: this.props.title
        };
    }

    onSelected = (e: ListActionEvent) => {
        const menuItem = this.props.menuItems[e.detail];
        menuItem.onSelected();
        this.setState({ title: menuItem.name });
        this.dropDownMenu.closeMenu();
    };

    render() {
        const list = (
            <List onAction={this.onSelected}>
                {this.props.menuItems.map(item => (
                    <ListItem key={item.name}>
                        {item.icon ? <ListItemGraphic icon={<MaterialIcon icon={item.icon} />} /> : null}
                        <ListItemText>{item.name}</ListItemText>
                    </ListItem>
                ))}
            </List>
        );

        return (
            <DropDownMenu
                ref={instance => {
                    this.dropDownMenu = instance;
                }}
                icon={this.props.icon}
                title={this.state.title}
            >
                {list}
            </DropDownMenu>
        );
    }
}
