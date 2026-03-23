// Filter list based on input
function filterList() {
    const input = document.getElementById('filterInput');
    const filter = document.getElementById('filterInput').value.toLowerCase();
    const list = document.getElementsByTagName("table")[0];
    const content = list.getElementsByTagName("tbody")[0].getElementsByTagName("tr");

    for (i = 0; i < content.length; i++) {
        let item = content[i].getElementsByTagName("td")[0];
        let textValue = item.textContent || item.innerText;
        if (textValue.toLowerCase().indexOf(filter) > -1) {
            content[i].style.display = "";
        } else {
            content[i].style.display = "none";
        }
    }
}