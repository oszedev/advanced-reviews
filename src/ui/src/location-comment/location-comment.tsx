import React, { useEffect, useRef } from "react";
import { inject } from "mobx-react";
import { TextField } from "@episerver/ui-framework";
import { IconButton } from "@episerver/ui-framework";
import MaterialIcon from "@material/react-material-icon";
import { DropDownMenu } from "../common/drop-down-menu";

import "./location-comment.scss";

interface LocationCommentProps {
    currentScreenshot: string;
    value: string;
    resources?: ReviewResources;
    onToggle: () => void;
    onChange: (comment: string, screenshot: string) => void;
    allowScreenshotAttachments: boolean;
}

const LocationComment = inject("resources")((props: LocationCommentProps) => {
    const commentInput = useRef(null);

    useEffect(() => {
        if (commentInput) {
            commentInput.current.focus();
        }
    });

    const resources = props.resources!;

    return (
        <>
            <TextField
                value={props.value}
                inputRef={commentInput}
                onChange={(e: React.FormEvent<any>) => props.onChange(e.currentTarget.value, props.currentScreenshot)}
                className="location-comment-field"
                label={`${resources.dialog.addcomment}...`}
                textarea={true}
            />
            {props.allowScreenshotAttachments && (
                <>
                    {!props.currentScreenshot && (
                        <IconButton
                            className="attach-screenshot"
                            title={resources.panel.attachscreenshot}
                            onClick={() => props.onToggle()}
                        >
                            <MaterialIcon icon="image" />
                        </IconButton>
                    )}
                    {props.currentScreenshot && (
                        <div className="attach-screenshot">
                            <DropDownMenu icon="image" title={resources.panel.showscreenshot}>
                                <img src={props.currentScreenshot} alt="screenshot" />
                            </DropDownMenu>
                            <IconButton
                                onClick={() => props.onChange(props.value, null)}
                                title={resources.panel.removescreenshot}
                            >
                                <MaterialIcon icon="remove" />
                            </IconButton>
                        </div>
                    )}
                </>
            )}
        </>
    );
});

export default LocationComment;
