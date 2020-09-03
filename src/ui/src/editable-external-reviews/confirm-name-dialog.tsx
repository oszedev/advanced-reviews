import React, { EventHandler, useState } from "react";

// import Dialog, { DialogTitle, DialogContent, DialogFooter, DialogButton } from "@material/react-dialog";
import { Dialog, DialogOnInteractionEvent, TextField } from "@episerver/ui-framework";

import "@material/react-dialog/index.scss";

interface ConfirmDialogProps {
    open: boolean;
    onClose(userName: string): void;
    initialUserName?: string;
}

const ConfirmDialog = ({ open, onClose, initialUserName }: ConfirmDialogProps) => {
    const [userName, setUserName] = useState<string>(initialUserName);

    return (
        <Dialog
            title="Confirm your name"
            open={open}
            onInteraction={({ detail: { action } }) => {
                if (action !== "save") {
                    onClose(null);
                    return;
                }

                onClose(userName);
            }}
            confirmLabel="Save"
            dismissLabel="Cancel"
            enableConfirm={!!userName}
        >
            <>
                <p>Please enter your name. It will be used as an author of the comments.</p>
                <div>
                    <TextField
                        onChange={(e: React.FormEvent<any>) => setUserName(e.currentTarget.value)}
                        value={userName}
                        label="Display name"
                        autoFocus
                        invalid={!userName}
                        required
                        style={{ width: "100%" }}
                    />
                </div>
            </>
        </Dialog>
    );
};

export default ConfirmDialog;
