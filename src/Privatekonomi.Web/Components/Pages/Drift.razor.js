export function submitForm(formId) {
    const form = document.getElementById(formId);
    if (!(form instanceof HTMLFormElement)) {
        throw new Error(`Could not find drift action form '${formId}'.`);
    }

    form.requestSubmit();
}
