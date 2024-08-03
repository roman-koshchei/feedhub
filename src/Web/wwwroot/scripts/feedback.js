/**
 * Depends on wave.js
 * Script specifically for feedback page
 */

ready(function () {
    /** @type {HTMLFormElement} */
    const form = document.getElementById("feedback-form")
    const list = document.getElementById("feedback-list")
    /** @type {HTMLInputElement} */
    const hideFormInput = document.getElementById("hide-create-form")

    form.addEventListener("submit", async function (event) {
        event.preventDefault()

        const formData = new FormData(form)
        if (event.submitter) {
            formData.append(event.submitter.name, event.submitter.value)
        }
        
        const res = await fetch(form.getAttribute("action"), {
            method: "POST",
            body: formData,
            headers: { WaveJavascript: "" },
        })

        const redirect = res.headers.get("WaveRedirect")
        if (redirect) {
            window.location.href = redirect
            return
        }

        const html = await res.text()
        if (res.redirected) {
            replaceDocumentHtml(html)
            pushCurrentDocumentToHistory(res.url)
        } else if (res.ok) {
            list.insertAdjacentHTML("afterbegin", html)
        } else {
            replaceDocumentHtml(html)
            pushCurrentDocumentToHistory(res.url)
        }

        form.reset()
        hideFormInput.checked = true
    })
})
