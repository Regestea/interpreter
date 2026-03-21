import React from 'react';
import { Modal } from 'react-bootstrap';

// تعریف تایپ پروپ‌ها برای امنیت کد (TypeScript)
interface BaseModalProps {
    show: boolean;
    onClose: () => void;
    title: string;
    children: React.ReactNode; // محتوای داخل مودال
    footer?: React.ReactNode;  // فوتر سفارشی (اختیاری)
    size?: 'sm' | 'lg' | 'xl'; // اندازه مودال
    centered?: boolean;
}

const BaseModal: React.FC<BaseModalProps> = ({show, onClose, title, children, footer, size = 'lg', centered = true}) => {
    return (
        <Modal
            className="modal"
            show={show}
            onHide={onClose}
            size={size}
            centered={centered}
            backdrop="static" // جلوگیری از بسته شدن با کلیک تصادفی به بیرون
        >
            <Modal.Header closeButton >
                <Modal.Title className="h5">{title}</Modal.Title>
            </Modal.Header>

            <Modal.Body >
                {children}
            </Modal.Body>

            {footer && (
                <Modal.Footer >
                    {footer}
                </Modal.Footer>
            )}
        </Modal>
    );
};

export default BaseModal;
