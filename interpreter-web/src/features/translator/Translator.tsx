import BaseModal from "../../common/components/BaseModal.tsx";
import {useState} from "react";


function Translator() {
    const [isModalOpen, setIsModalOpen] = useState<boolean>(false);

    const toggleModal = () => setIsModalOpen(!isModalOpen);


    const modalFooter = (
        <>
          <p>modal footer</p>
        </>
    );
    
    return <>
        <button className="btn btn-primary" onClick={toggleModal}>open modal</button>
        <BaseModal
            show={isModalOpen}
            onClose={toggleModal}
            title="are you ok with delete ?"
            size="sm"
            footer={modalFooter}
        >
            <p>are sure want to delete this content</p>
        </BaseModal>
    </>;
}

export default Translator;